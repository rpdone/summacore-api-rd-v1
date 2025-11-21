using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummaCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace SummaCore.Controllers
{
    [Route("api/pruebas")]
    [ApiController]
    public class PruebasController : ControllerBase
    {
        private readonly ILogger<PruebasController> _logger;
        private readonly SignerService _signer;
        private readonly DgiiService _dgiiSender;
        private readonly IConfiguration _configuration;

        public PruebasController(
            ILogger<PruebasController> logger,
            SignerService signer,
            DgiiService dgiiSender,
            IConfiguration configuration)
        {
            _logger = logger;
            _signer = signer;
            _dgiiSender = dgiiSender;
            _configuration = configuration;
        }

        // =================================================================================
        // 1. CSV UPLOAD & PROCESS ENDPOINT
        // =================================================================================

        [HttpPost("cargar-csv-paso2")]
        public async Task<IActionResult> CargarCsvPaso2(IFormFile archivoCsv)
        {
            if (archivoCsv == null || archivoCsv.Length == 0)
                return BadRequest("Por favor sube un archivo CSV válido.");

            var reporte = new List<object>();
            
            // Load Certificate Configuration
            var rncCertificado = _configuration["Dgii:Rnc"] ?? "130495527"; // Default if not in config
            var certPath = _configuration["Dgii:CertificatePath"] ?? "13049552_identity.p12";
            var certPass = _configuration["Dgii:CertificatePassword"] ?? "Solutecdo2025@";

            // Locate certificate file
            var fullCertPath = Path.Combine(Directory.GetCurrentDirectory(), certPath);
            if (!System.IO.File.Exists(fullCertPath))
            {
                // Fallback to bin directory if running locally
                fullCertPath = Path.Combine(AppContext.BaseDirectory, certPath);
                if (!System.IO.File.Exists(fullCertPath))
                {
                    return BadRequest($"No se encuentra el certificado en: {certPath}");
                }
            }

            try
            {
                _logger.LogInformation("=== INICIANDO PROCESO MASIVO CSV ===");

                // 1. Authenticate once for the batch (optional optimization, or auth per request inside DgiiService)
                var certificate = new X509Certificate2(fullCertPath, certPass);
                var token = await _dgiiSender.Autenticar(certificate);
                _logger.LogInformation("✓ Autenticación exitosa.");

                // 2. Read and Process CSV
                using (var reader = new StreamReader(archivoCsv.OpenReadStream(), Encoding.UTF8))
                {
                    // Read Header
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(headerLine)) return BadRequest("El CSV está vacío.");

                    // Detect delimiter (; or ,)
                    char delimiter = headerLine.Contains(';') ? ';' : ',';
                    
                    // Parse Header
                    var headers = SplitCsvLine(headerLine, delimiter);
                    var map = MapearColumnas(headers);

                    // Basic Validation
                    if (!map.ContainsKey("ENCF") && !map.ContainsKey("eNCF"))
                    {
                        return BadRequest("Formato CSV inválido: Falta columna 'ENCF'.");
                    }

                    int lineaNum = 1;
                    while (!reader.EndOfStream)
                    {
                        lineaNum++;
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Handle multi-line CSV rows if necessary (simple implementation assumes single line)
                        // For robust CSV parsing, consider using CsvHelper library in production.
                        var cols = SplitCsvLine(line, delimiter);

                        if (cols.Length < map.Count / 2) // Rough check for malformed lines
                        {
                            reporte.Add(new { linea = lineaNum, estado = "ERROR", mensaje = "Línea incompleta o malformada." });
                            continue;
                        }

                        var dto = ParsearLinea(cols, map);

                        // Logic: Skip Invoice Consumer < 250k
                        bool esConsumo = dto.TipoeCF == 32;
                        bool menor250k = dto.MontoTotal < 250000m;

                        if (esConsumo && menor250k)
                        {
                            reporte.Add(new { 
                                linea = lineaNum, 
                                encf = dto.Encf, 
                                estado = "SKIPPED", 
                                mensaje = "Factura Consumo < 250k. Requiere envío por Resumen (RFCE)." 
                            });
                            continue;
                        }

                        try
                        {
                            // Process and Send Individual e-CF
                            var response = await ProcesarYEnviar(dto, certificate, token, rncCertificado);
                            
                            // Simple check if response looks like success (contains trackId)
                            bool success = response.Contains("trackId") || response.Contains("TrackId");
                            
                            reporte.Add(new { 
                                linea = lineaNum, 
                                encf = dto.Encf, 
                                estado = success ? "ENVIADO" : "RECHAZADO", 
                                respuesta = response 
                            });
                        }
                        catch (Exception ex)
                        {
                            reporte.Add(new { 
                                linea = lineaNum, 
                                encf = dto.Encf, 
                                estado = "ERROR", 
                                error = ex.Message 
                            });
                            _logger.LogError(ex, $"Error processing line {lineaNum}: {dto.Encf}");
                        }
                    }
                }

                return Ok(new { mensaje = "Proceso completado", resultados = reporte });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en carga masiva");
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
        }

        // =================================================================================
        // 2. HELPER METHODS FOR CSV & XML MAPPING
        // =================================================================================

        private async Task<string> ProcesarYEnviar(CsvEcfDto dto, X509Certificate2 cert, string token, string rncEmisor)
        {
            var ecf = new ECF();
            
            // --- 1. Encabezado ---
            ecf.Encabezado.Version = "1.0";
            ecf.Encabezado.IdDoc.TipoeCF = dto.TipoeCF;
            ecf.Encabezado.IdDoc.eNCF = dto.Encf;
            ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = string.IsNullOrWhiteSpace(dto.FechaVencimientoSecuencia) ? "31-12-2025" : dto.FechaVencimientoSecuencia;
            ecf.Encabezado.IdDoc.IndicadorMontoGravado = dto.IndicadorMontoGravado;
            ecf.Encabezado.IdDoc.TipoIngresos = dto.TipoIngresos;
            ecf.Encabezado.IdDoc.TipoPago = dto.TipoPago == 0 ? 1 : dto.TipoPago; // Default to 1 (Cash) if 0
            ecf.Encabezado.IdDoc.FechaLimitePago = string.IsNullOrWhiteSpace(dto.FechaLimitePago) ? DateTime.Now.ToString("dd-MM-yyyy") : dto.FechaLimitePago;

            ecf.Encabezado.Emisor.RNCEmisor = rncEmisor;
            ecf.Encabezado.Emisor.RazonSocialEmisor = string.IsNullOrWhiteSpace(dto.RazonSocialEmisor) ? "RAZON SOCIAL DEFAULT" : dto.RazonSocialEmisor;
            ecf.Encabezado.Emisor.DireccionEmisor = string.IsNullOrWhiteSpace(dto.DireccionEmisor) ? "CALLE PRINCIPAL 123" : dto.DireccionEmisor;
            ecf.Encabezado.Emisor.FechaEmision = string.IsNullOrWhiteSpace(dto.FechaEmision) || dto.FechaEmision.Contains("#") ? DateTime.Now.ToString("dd-MM-yyyy") : dto.FechaEmision;

            ecf.Encabezado.Comprador.RNCComprador = dto.RNCComprador;
            ecf.Encabezado.Comprador.RazonSocialComprador = string.IsNullOrWhiteSpace(dto.RazonSocialComprador) ? "CONSUMIDOR FINAL" : dto.RazonSocialComprador;

            // --- 2. Totales ---
            ecf.Encabezado.Totales.MontoTotal = dto.MontoTotal;
            ecf.Encabezado.Totales.MontoGravadoTotal = dto.MontoGravadoTotal;
            ecf.Encabezado.Totales.MontoExento = dto.MontoExento;
            ecf.Encabezado.Totales.TotalITBIS = dto.TotalITBIS;

            // --- 3. Detalle Items (Synthetic creation based on totals) ---
            int lineNum = 1;
            
            // 18% ITBIS Items
            if (dto.MontoGravadoI1 > 0)
            {
                ecf.Encabezado.Totales.MontoGravadoI1 = dto.MontoGravadoI1;
                ecf.Encabezado.Totales.ITBIS1 = dto.ITBIS1;
                ecf.Encabezado.Totales.TotalITBIS1 = dto.ITBIS1; // Set accumulated total for 18%

                ecf.DetallesItems.Item.Add(CreateItem(lineNum++, "Servicios/Bienes al 18%", 2, dto.MontoGravadoI1, 1));
            }

            // 16% ITBIS Items
            if (dto.MontoGravadoI2 > 0)
            {
                ecf.Encabezado.Totales.MontoGravadoI2 = dto.MontoGravadoI2;
                ecf.Encabezado.Totales.ITBIS2 = dto.ITBIS2;
                ecf.Encabezado.Totales.TotalITBIS2 = dto.ITBIS2; // Set accumulated total for 16%

                ecf.DetallesItems.Item.Add(CreateItem(lineNum++, "Servicios/Bienes al 16%", 2, dto.MontoGravadoI2, 2));
            }

            // Exempt Items
            if (dto.MontoExento > 0)
            {
                // Indicator 1 = Exento (according to DGII table 11)
                ecf.DetallesItems.Item.Add(CreateItem(lineNum++, "Servicios/Bienes Exentos", 1, dto.MontoExento, 0));
            }
            
            // --- 4. Subtotales (Mandatory) ---
            // We aggregate everything into one subtotal line for simplicity, matching the invoice totals
            ecf.Subtotales.Subtotal.Add(new Subtotal
            {
                NumeroSubTotal = 1,
                DescripcionSubtotal = "Subtotal General",
                Orden = 1,
                SubTotalMontoGravadoTotal = dto.MontoGravadoTotal,
                SubTotalMontoGravadoI1 = dto.MontoGravadoI1,
                SubTotalMontoGravadoI2 = dto.MontoGravadoI2,
                SubTotalExento = dto.MontoExento,
                SubTotaITBIS = dto.TotalITBIS,
                SubTotaITBIS1 = dto.ITBIS1,
                SubTotaITBIS2 = dto.ITBIS2,
                MontoSubTotal = dto.MontoTotal,
                Lineas = lineNum - 1 // Total count of items added
            });

            // --- 5. FechaHoraFirma (Mandatory at end of XML) ---
            ecf.FechaHoraFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            // --- 6. Serialize & Sign ---
            var sw = new Utf8StringWriter();
            var serializer = new XmlSerializer(typeof(ECF));
            serializer.Serialize(sw, ecf);
            string xmlSinFirma = sw.ToString();

            // Sign the XML
            string xmlFirmado = _signer.FirmarECF(xmlSinFirma, cert);

            // Send to DGII
            return await _dgiiSender.EnviarFactura(xmlFirmado, token, $"{dto.Encf}.xml");
        }

        private Item CreateItem(int lineNumber, string name, int indicator, decimal amount, int taxType)
        {
            var item = new Item
            {
                NumeroLinea = lineNumber,
                NombreItem = name,
                DescripcionItem = name,
                IndicadorFacturacion = indicator,
                CantidadItem = 1, // Default quantity 1
                PrecioUnitarioItem = amount, // Price = Total Amount for simplicity
                MontoItem = amount,
                // IndicadorBienoServicio: 1=Goods, 2=Services. Defaulting to 2 (Services)
                IndicadorBienoServicio = 2 
            };

            if (taxType > 0)
            {
                item.TablaImpuestoAdicional = new List<ImpuestoAdicional>
                {
                    new ImpuestoAdicional { TipoImpuesto = taxType }
                };
            }

            return item;
        }

        // Helper to split CSV line handling quotes
        private string[] SplitCsvLine(string line, char delimiter)
        {
            // This regex handles values inside quotes that may contain the delimiter
            // e.g. "Smith, John", 25, "New York, NY" -> ["Smith, John", "25", "New York, NY"]
            var pattern = string.Format("{0}(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", delimiter);
            var parts = System.Text.RegularExpressions.Regex.Split(line, pattern);
            
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim().Trim('"'); // Remove quotes and whitespace
            }
            return parts;
        }

        // Mapping column headers to indices
        private Dictionary<string, int> MapearColumnas(string[] headers)
        {
            var map = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                // Normalize header: remove quotes, BOM, whitespace
                string h = headers[i].Trim().Trim('"').Replace("\uFEFF", "");
                if (!string.IsNullOrEmpty(h))
                {
                    map[h] = i;
                }
            }
            return map;
        }

        // Convert CSV row to DTO object
        private CsvEcfDto ParsearLinea(string[] cols, Dictionary<string, int> map)
        {
            string GetVal(string key)
            {
                if (map.ContainsKey(key) && map[key] < cols.Length) return cols[map[key]];
                // Try uppercase if not found
                if (map.ContainsKey(key.ToUpper()) && map[key.ToUpper()] < cols.Length) return cols[map[key.ToUpper()]];
                return "";
            }

            decimal GetDec(string key)
            {
                string s = GetVal(key);
                if (string.IsNullOrEmpty(s) || s == "#e") return 0m;
                s = s.Replace("$", "").Replace(",", "").Trim(); // Clean currency
                return decimal.TryParse(s, out decimal d) ? d : 0m;
            }

            int GetInt(string key)
            {
                string s = GetVal(key);
                return int.TryParse(s, out int i) ? i : 0;
            }

            return new CsvEcfDto
            {
                Encf = GetVal("ENCF"),
                TipoeCF = GetInt("TipoeCF"),
                FechaVencimientoSecuencia = GetVal("FechaVencimientoSecuencia"),
                IndicadorMontoGravado = GetInt("IndicadorMontoGravado"),
                TipoIngresos = GetInt("TipoIngresos"),
                TipoPago = GetInt("TipoPago"),
                FechaLimitePago = GetVal("FechaLimitePago"),
                RNCEmisor = GetVal("RNCEmisor"),
                RazonSocialEmisor = GetVal("RazonSocialEmisor"),
                DireccionEmisor = GetVal("DireccionEmisor"),
                FechaEmision = GetVal("FechaEmision"),
                RNCComprador = GetVal("RNCComprador"),
                RazonSocialComprador = GetVal("RazonSocialComprador"),
                MontoTotal = GetDec("MontoTotal"),
                MontoGravadoTotal = GetDec("MontoGravadoTotal"),
                MontoGravadoI1 = GetDec("MontoGravadoI1"),
                MontoGravadoI2 = GetDec("MontoGravadoI2"),
                MontoExento = GetDec("MontoExento"),
                TotalITBIS = GetDec("TotalITBIS"),
                ITBIS1 = GetDec("ITBIS1") != 0 ? GetDec("ITBIS1") : GetDec("TotalITBIS1"),
                ITBIS2 = GetDec("ITBIS2") != 0 ? GetDec("ITBIS2") : GetDec("TotalITBIS2")
            };
        }

        // ======================================================
        // EXISTING METHODS (PRESERVED)
        // ======================================================

        [HttpGet("test-autenticacion")]
        public async Task<IActionResult> TestAutenticacion()
        {
            try
            {
                _logger.LogInformation("=== INICIANDO TEST DE AUTENTICACIÓN ===");
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");

                if (!System.IO.File.Exists(rutaCert))
                {
                    return BadRequest(new { error = "Certificado no encontrado", ruta = rutaCert });
                }

                var passCert = "Solutecdo2025@";
                var certificado = new X509Certificate2(rutaCert, passCert);
                var token = await _dgiiSender.Autenticar(certificado);

                return Ok(new { mensaje = "Autenticación exitosa", token = token.Substring(0, Math.Min(50, token.Length)) + "..." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet("ejecutar-paso2")]
        public async Task<IActionResult> EjecutarPaso2()
        {
            try
            {
                _logger.LogInformation("=== INICIANDO PROCESO COMPLETO (EJEMPLO MANUAL) ===");
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");
                var certificado = new X509Certificate2(rutaCert, "Solutecdo2025@");

                _logger.LogInformation("[PASO 1] Autenticando...");
                var token = await _dgiiSender.Autenticar(certificado);
                _logger.LogInformation("✓ Token obtenido");

                _logger.LogInformation("[PASO 2] Generando factura de prueba...");

                decimal precio = 1500.00m;
                decimal cantidad = 1.00m;
                decimal total = precio * cantidad;

                var factura = new ECF
                {
                    Encabezado = new Encabezado
                    {
                        Version = "1.0",
                        IdDoc = new IdDoc
                        {
                            TipoeCF = 31,
                            eNCF = "E310000000003", // CAMBIAR PARA EVITAR DUPLICADOS
                            FechaVencimientoSecuencia = "31-12-2025",
                            IndicadorMontoGravado = 0,
                            TipoIngresos = 1
                        },
                        Emisor = new Emisor
                        {
                            RNCEmisor = "131487272",
                            RazonSocialEmisor = "TU EMPRESA S.R.L.",
                            FechaEmision = DateTime.Now.ToString("dd-MM-yyyy"),
                            DireccionEmisor = "Calle Ficticia 123"
                        },
                        Comprador = new Comprador
                        {
                            RNCComprador = "101796361",
                            RazonSocialComprador = "CLIENTE DE PRUEBA"
                        },
                        Totales = new Totales
                        {
                            MontoTotal = total,
                            MontoExento = total
                        }
                    },
                    DetallesItems = new DetallesItems
                    {
                        Item = new List<Item>
                        {
                            new Item
                            {
                                NumeroLinea = 1,
                                NombreItem = "Servicio de Desarrollo de Software",
                                IndicadorFacturacion = 1,
                                IndicadorBienoServicio = 2,
                                CantidadItem = cantidad,
                                PrecioUnitarioItem = precio,
                                MontoItem = total
                            }
                        }
                    },
                    Subtotales = new Subtotales
                    {
                        Subtotal = new List<Subtotal>
                        {
                            new Subtotal
                            {
                                NumeroSubTotal = 1,
                                DescripcionSubtotal = "Subtotal",
                                Orden = 1,
                                SubTotalExento = total,
                                MontoSubTotal = total
                            }
                        }
                    },
                    FechaHoraFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                };

                var sw = new Utf8StringWriter();
                var serializer = new XmlSerializer(typeof(ECF));
                serializer.Serialize(sw, factura);
                string xmlSinFirma = sw.ToString();

                string xmlFirmado = _signer.FirmarECF(xmlSinFirma, certificado);

                _logger.LogInformation("[PASO 4] Enviando a DGII...");
                string resultado = await _dgiiSender.EnviarFactura(xmlFirmado, token, "E310000000003.xml");

                return Ok(new { mensaje = "Proceso completado", respuestaDgii = resultado });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        
        [HttpGet("test-firma")]
        public IActionResult TestFirma()
        {
            try
            {
                string xmlPrueba = "<SemillaModel><valor>123</valor></SemillaModel>";
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");
                var certificado = new X509Certificate2(rutaCert, "Solutecdo2025@");
                var xmlFirmado = _signer.FirmarECF(xmlPrueba, certificado);
                return Ok(new { mensaje = "Firma exitosa", xml = xmlFirmado });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }

    // ==========================================
    // XML MODELS - UPDATED STRUCTURE
    // ==========================================

    [XmlRoot(ElementName = "ECF", Namespace = "")]
    public class ECF
    {
        [XmlElement(Order = 1)]
        public Encabezado Encabezado { get; set; } = new Encabezado();

        [XmlElement(Order = 2)]
        public DetallesItems DetallesItems { get; set; } = new DetallesItems();

        [XmlElement(Order = 3)]
        public Subtotales Subtotales { get; set; } = new Subtotales();

        [XmlElement(Order = 4)]
        public string FechaHoraFirma { get; set; }
    }

    public class Encabezado
    {
        [XmlElement(Order = 1)]
        public string Version { get; set; } = "1.0";

        [XmlElement(Order = 2)]
        public IdDoc IdDoc { get; set; } = new IdDoc();

        [XmlElement(Order = 3)]
        public Emisor Emisor { get; set; } = new Emisor();

        [XmlElement(Order = 4)]
        public Comprador Comprador { get; set; } = new Comprador();

        [XmlElement(Order = 5)]
        public Totales Totales { get; set; } = new Totales();
    }

    public class IdDoc
    {
        [XmlElement(Order = 1)]
        public int TipoeCF { get; set; }
        [XmlElement(Order = 2)]
        public string eNCF { get; set; } = string.Empty;
        [XmlElement(Order = 3)]
        public string FechaVencimientoSecuencia { get; set; } = string.Empty;
        [XmlElement(Order = 4)]
        public int IndicadorMontoGravado { get; set; } = 0;
        [XmlElement(Order = 5)]
        public int TipoIngresos { get; set; } = 1;
        [XmlElement(Order = 6)]
        public int TipoPago { get; set; } = 1;
        [XmlElement(Order = 7)]
        public string FechaLimitePago { get; set; } = string.Empty;
    }

    public class Emisor
    {
        [XmlElement(Order = 1)]
        public string RNCEmisor { get; set; } = string.Empty;
        [XmlElement(Order = 2)]
        public string RazonSocialEmisor { get; set; } = string.Empty;
        [XmlElement(Order = 5)]
        public string DireccionEmisor { get; set; } = string.Empty;
        [XmlElement(Order = 18)]
        public string FechaEmision { get; set; } = string.Empty;
    }

    public class Comprador
    {
        [XmlElement(Order = 1)]
        public string RNCComprador { get; set; } = string.Empty;
        [XmlElement(Order = 2)]
        public string RazonSocialComprador { get; set; } = string.Empty;
    }

    public class Totales
    {
        [XmlElement(Order = 1)]
        public decimal MontoGravadoTotal { get; set; }
        [XmlElement(Order = 2)]
        public decimal MontoGravadoI1 { get; set; }
        [XmlElement(Order = 3)]
        public decimal MontoGravadoI2 { get; set; }
        [XmlElement(Order = 5)]
        public decimal MontoExento { get; set; }
        [XmlElement(Order = 6)]
        public decimal ITBIS1 { get; set; }
        [XmlElement(Order = 7)]
        public decimal ITBIS2 { get; set; }
        [XmlElement(Order = 9)]
        public decimal TotalITBIS { get; set; }
        [XmlElement(Order = 10)]
        public decimal TotalITBIS1 { get; set; }
        [XmlElement(Order = 11)]
        public decimal TotalITBIS2 { get; set; }
        [XmlElement(Order = 15)]
        public decimal MontoTotal { get; set; }
    }

    public class DetallesItems
    {
        [XmlElement("Item")]
        public List<Item> Item { get; set; } = new List<Item>();
    }

    public class Item
    {
        [XmlElement(Order = 1)]
        public int NumeroLinea { get; set; }
        [XmlElement(Order = 3)]
        public int IndicadorFacturacion { get; set; }
        [XmlElement(Order = 5)]
        public string NombreItem { get; set; } = string.Empty;
        [XmlElement(Order = 6)]
        public int IndicadorBienoServicio { get; set; }
        [XmlElement(Order = 7)]
        public string DescripcionItem { get; set; } = string.Empty;
        [XmlElement(Order = 8)]
        public decimal CantidadItem { get; set; }
        [XmlElement(Order = 15)]
        public decimal PrecioUnitarioItem { get; set; }
        [XmlArray("TablaImpuestoAdicional", Order = 19)]
        [XmlArrayItem("ImpuestoAdicional")]
        public List<ImpuestoAdicional> TablaImpuestoAdicional { get; set; }
        [XmlElement(Order = 21)]
        public decimal MontoItem { get; set; }
    }

    public class ImpuestoAdicional
    {
        public int TipoImpuesto { get; set; }
    }

    public class Subtotales
    {
        [XmlElement("Subtotal")]
        public List<Subtotal> Subtotal { get; set; } = new List<Subtotal>();
    }

    public class Subtotal
    {
        [XmlElement(Order = 1)]
        public int NumeroSubTotal { get; set; }
        [XmlElement(Order = 2)]
        public string DescripcionSubtotal { get; set; } = string.Empty;
        [XmlElement(Order = 3)]
        public int Orden { get; set; }
        [XmlElement(Order = 4)]
        public decimal SubTotalMontoGravadoTotal { get; set; }
        [XmlElement(Order = 5)]
        public decimal SubTotalMontoGravadoI1 { get; set; }
        [XmlElement(Order = 6)]
        public decimal SubTotalMontoGravadoI2 { get; set; }
        [XmlElement(Order = 7)]
        public decimal SubTotalExento { get; set; }
        [XmlElement(Order = 8)]
        public decimal SubTotaITBIS { get; set; }
        [XmlElement(Order = 9)]
        public decimal SubTotaITBIS1 { get; set; }
        [XmlElement(Order = 10)]
        public decimal SubTotaITBIS2 { get; set; }
        [XmlElement(Order = 14)]
        public decimal MontoSubTotal { get; set; }
        [XmlElement(Order = 15)]
        public int Lineas { get; set; }
    }

    public class CsvEcfDto
    {
        public string Encf { get; set; }
        public int TipoeCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public int IndicadorMontoGravado { get; set; }
        public int TipoIngresos { get; set; }
        public int TipoPago { get; set; }
        public string FechaLimitePago { get; set; }
        public string RNCEmisor { get; set; }
        public string RazonSocialEmisor { get; set; }
        public string DireccionEmisor { get; set; }
        public string FechaEmision { get; set; }
        public string RNCComprador { get; set; }
        public string RazonSocialComprador { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoGravadoTotal { get; set; }
        public decimal MontoGravadoI1 { get; set; }
        public decimal MontoGravadoI2 { get; set; }
        public decimal MontoExento { get; set; }
        public decimal TotalITBIS { get; set; }
        public decimal ITBIS1 { get; set; }
        public decimal ITBIS2 { get; set; }
        public decimal TotalITBIS1 { get; set; }
        public decimal TotalITBIS2 { get; set; }
    }

    // Utf8StringWriter ensures XmlSerializer outputs UTF-8 encoding when serializing to a string.
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}