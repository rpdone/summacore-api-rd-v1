using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ClosedXML.Excel;
using SummaCore.Models;
using SummaCore.Services;
using SummaCore.Validation;

namespace SummaCore.Processor
{
    public class ECFProcessor
    {
        private readonly DgiiService _dgiiService;
        private readonly SignerService _signerService;

        public ECFProcessor(DgiiService dgiiService, SignerService signerService)
        {
            _dgiiService = dgiiService;
            _signerService = signerService;
        }

        public async Task ProcessExcelFile(string filePath, string outputDir)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found at {filePath}");
                return;
            }

            Console.WriteLine($"Processing file: {filePath}");
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet("ECF");
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

            int passed = 0;
            int failed = 0;
            int submitted = 0;
            string authToken = null;

            foreach (var row in rows)
            {
                Console.WriteLine($"Processing Row {row.RowNumber()}...");
                try
                {
                    var ecf = MapRowToECF(row);
                    
                    // Validate
                    var validator = new ECFValidator();
                    var validationErrors = validator.Validar(ecf);
                    
                    if (validationErrors.Count > 0)
                    {
                        Console.WriteLine($"  Status: INVALID");
                        foreach (var error in validationErrors)
                        {
                            Console.WriteLine($"      - {error}");
                        }
                        failed++;
                    }
                    else
                    {
                        Console.WriteLine($"  Status: VALID");
                        passed++;

                        // Serialize
                        var xmlContent = SerializarObjeto(ecf);
                        System.IO.File.WriteAllText("debug_ecf.xml", xmlContent); // DEBUG: Save XML to file

                        var fileName = $"{ecf.Encabezado.Emisor.RNCEmisor}_{ecf.Encabezado.IdDoc.eNCF}_{ecf.Encabezado.IdDoc.TipoeCF}.xml";
                        var fullPath = Path.Combine(outputDir, fileName);
                        // File.WriteAllText(fullPath, xmlContent); // Optional: save unsigned

                        // Sign
                        Console.WriteLine("  Signing and Submitting...");
                        var signedXml = _signerService.SignXML(xmlContent);

                        // Submit
                        var result = await _dgiiService.Facturar(ecf);
                        Console.WriteLine($"  DGII Response: {result.Response}");
                        
                        if (!string.IsNullOrEmpty(result.Token))
                        {
                            authToken = result.Token;
                        }

                        submitted++;

                        // Parse TrackId
                        string trackId = null;
                        try {
                            using (var doc = System.Text.Json.JsonDocument.Parse(result.Response)) {
                                if (doc.RootElement.TryGetProperty("trackId", out var trackIdElement)) {
                                    trackId = trackIdElement.GetString();
                                }
                            }
                        } catch {}

                        if (!string.IsNullOrEmpty(trackId)) {
                            Console.WriteLine($"  TrackId: {trackId} - Waiting 2 seconds before checking status...");
                            await Task.Delay(2000);
                            var statusResponse = await _dgiiService.ConsultarEstado(trackId, authToken);
                            Console.WriteLine($"RAW RESPONSE: {statusResponse}");
                            
                            // Parse and log status details
                            try {
                                using (var doc = System.Text.Json.JsonDocument.Parse(statusResponse)) {
                                    var root = doc.RootElement;
                                    var estado = root.TryGetProperty("estado", out var e) ? e.GetString() : "Unknown";
                                    Console.WriteLine($"  Status Response: {estado}");
                                    
                                    if (root.TryGetProperty("mensajes", out var mensajes) && mensajes.ValueKind == System.Text.Json.JsonValueKind.Array) {
                                        foreach (var msg in mensajes.EnumerateArray()) {
                                            var texto = msg.TryGetProperty("mensaje", out var t) ? t.GetString() : "NO MESSAGE";
                                            Console.WriteLine($"!!! ERROR [Row {row.RowNumber()}, Type {ecf.Encabezado.IdDoc.TipoeCF}]: {texto} !!!");
                                        }
                                    }
                                }
                            } catch {
                                Console.WriteLine($"  Raw Status Response: {statusResponse}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error processing row {row.RowNumber()}: {ex.Message}");
                    failed++;
                }
                Console.WriteLine(new string('-', 20));
            }
            Console.WriteLine($"Summary: {passed} Validated, {submitted} Submitted, {failed} Failed.");
        }

        private string SerializarObjeto(ECF ecf)
        {
            var serializer = new XmlSerializer(typeof(ECF));
            var namespaces = new XmlSerializerNamespaces(); namespaces.Add("", "");
            using (var stringWriter = new StringWriterUtf8()) { serializer.Serialize(stringWriter, ecf, namespaces); return stringWriter.ToString(); }
        }

        private ECF MapRowToECF(IXLRangeRow row)
        {
            var ecf = new ECF();
            
            // Helper to get string value safely
            string GetString(string header) {
                var cell = row.Worksheet.Row(1).CellsUsed(c => c.Value.ToString() == header).FirstOrDefault();
                if (cell == null) return null;
                // IXLRangeRow cells are relative to the range, but here we access by column number from the worksheet
                var val = row.Cell(cell.Address.ColumnNumber).Value.ToString();
                return val == "#e" ? null : val;
            }

            // Helper for int
            int? GetInt(string header) {
                var s = GetString(header);
                return int.TryParse(s, out int i) ? i : null;
            }

            // Helper for decimal
            decimal? GetDecimal(string header) {
                var s = GetString(header);
                return decimal.TryParse(s, out decimal d) ? d : null;
            }

             // Helper for date
            string GetDate(string header) {
                 var s = GetString(header);
                 if (string.IsNullOrEmpty(s)) return null;
                 if (double.TryParse(s, out double d))
                    return DateTime.FromOADate(d).ToString("dd-MM-yyyy");
                 if (DateTime.TryParse(s, out DateTime dt))
                    return dt.ToString("dd-MM-yyyy");
                 return s; 
            }


            // Mapping
            ecf.Encabezado.IdDoc.TipoeCF = GetInt("TipoeCF") ?? 0;
            ecf.Encabezado.IdDoc.eNCF = GetString("ENCF") ?? "";
            ecf.Encabezado.Emisor.FechaEmision = DateTime.Now.ToString("dd-MM-yyyy"); 
            ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = GetDate("FechaVencimientoSecuencia");
            ecf.Encabezado.IdDoc.TipoIngresosRaw = GetString("TipoIngresos"); 
            ecf.Encabezado.IdDoc.TipoPago = GetInt("TipoPago");
            
            // Fix for Type 34 (Nota de Credito) - Requires NCFModificado
            if (ecf.Encabezado.IdDoc.TipoeCF == 34)
            {
                var ncfModificado = GetString("NCFModificado");
                if (!string.IsNullOrEmpty(ncfModificado))
                {
                    ecf.InformacionReferencia = new InformacionReferencia
                    {
                        NCFModificado = ncfModificado,
                        FechaNCFModificado = DateTime.Now.ToString("dd-MM-yyyy"), 
                        CodigoModificacion = GetInt("CodigoModificacion") ?? 3 
                    };
                }
            }
            
            // Legacy check fix (just in case)
             if (ecf.Encabezado.IdDoc.TipoeCF == 33 || ecf.Encabezado.IdDoc.TipoeCF == 34)
            {
            }

             // Fix for Type 43 (Gastos Menores)
            if (ecf.Encabezado.IdDoc.TipoeCF == 43 && ecf.Encabezado.IdDoc.TipoPago == null)
            {
                 ecf.Encabezado.IdDoc.TipoPago = 1; 
            }


            ecf.FechaHoraFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            ecf.Encabezado.Emisor.RNCEmisor = GetString("RNCEmisor") ?? "";
            ecf.Encabezado.Emisor.RazonSocialEmisor = GetString("RazonSocialEmisor") ?? "";
            ecf.Encabezado.Emisor.NombreComercial = GetString("NombreComercial");
            ecf.Encabezado.Emisor.Sucursal = GetString("Sucursal");
            ecf.Encabezado.Emisor.DireccionEmisor = GetString("DireccionEmisor") ?? "Calle Principal";
            ecf.Encabezado.Emisor.Municipio = "320100"; 
            ecf.Encabezado.Emisor.Provincia = "320000"; 
            ecf.Encabezado.Emisor.TablaTelefonoEmisor = new List<string> { "8095555555" };
            ecf.Encabezado.Emisor.CorreoEmisor = "facturacion@example.com";

            ecf.Encabezado.Comprador.RNCComprador = GetString("RNCComprador") ?? "";
            ecf.Encabezado.Comprador.RazonSocialComprador = GetString("RazonSocialComprador") ?? "";
            ecf.Encabezado.Comprador.IdentificadorExtranjero = GetString("IdentificadorExtranjero");

            // Security Code (Placeholder for now, usually derived from signature or random)
            // ecf.Encabezado.CodigoSeguridadeCF = "123456";

            // Force OtraMoneda for ALL types to test schema
            ecf.Encabezado.OtraMoneda = new OtraMoneda
            {
                TipoMoneda = "USD",
                TipoCambio = 58.00m
            };

            ecf.Encabezado.Totales.MontoGravadoTotal = GetDecimal("MontoGravadoTotal");
            ecf.Encabezado.Totales.MontoGravadoI1 = GetDecimal("MontoGravadoI1");
            ecf.Encabezado.Totales.MontoExento = GetDecimal("MontoExento");
            ecf.Encabezado.Totales.TotalITBIS = GetDecimal("TotalITBIS");
            ecf.Encabezado.Totales.TotalITBIS1 = GetDecimal("TotalITBIS1");
            ecf.Encabezado.Totales.MontoImpuestoAdicional = GetDecimal("MontoImpuestoAdicional");
            ecf.Encabezado.Totales.MontoTotal = GetDecimal("MontoTotal") ?? ((ecf.Encabezado.Totales.MontoGravadoTotal ?? 0) + (ecf.Encabezado.Totales.TotalITBIS ?? 0) + (ecf.Encabezado.Totales.MontoExento ?? 0) + (ecf.Encabezado.Totales.MontoImpuestoAdicional ?? 0));

            // Items
            ecf.DetallesItems.Item.Add(new Item
            {
                NumeroLinea = 1,
                NombreItem = "Servicios Profesionales",
                CantidadItem = 1,
                PrecioUnitarioItem = (ecf.Encabezado.Totales.MontoGravadoTotal ?? 0) + (ecf.Encabezado.Totales.MontoExento ?? 0),
                MontoItem = (ecf.Encabezado.Totales.MontoGravadoTotal ?? 0) + (ecf.Encabezado.Totales.MontoExento ?? 0)
            });

            return ecf;
        }
    }

    public class StringWriterUtf8 : StringWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
    }
}
