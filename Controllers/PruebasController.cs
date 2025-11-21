using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummaCore.Services;
using SummaCore.Models; // Usando el modelo corregido
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using System.Text;

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

        [HttpPost("cargar-csv-paso2")]
        public async Task<IActionResult> CargarCsvPaso2(IFormFile archivoCsv)
        {
            if (archivoCsv == null || archivoCsv.Length == 0)
                return BadRequest("Por favor sube un archivo CSV o Excel válido.");

            var reporte = new List<object>();
            var rncCertificado = _configuration["Dgii:Rnc"] ?? "131487272";
            var certPath = _configuration["Dgii:CertificatePath"] ?? "13049552_identity.p12";
            var certPass = _configuration["Dgii:CertificatePassword"] ?? "Solutecdo2025@";

            var fullCertPath = Path.Combine(Directory.GetCurrentDirectory(), certPath);
            if (!System.IO.File.Exists(fullCertPath))
                fullCertPath = Path.Combine(AppContext.BaseDirectory, certPath);

            try
            {
                _logger.LogInformation("=== INICIANDO PROCESO MASIVO ===");
                #pragma warning disable SYSLIB0057
                var certificate = new X509Certificate2(fullCertPath, certPass);
                #pragma warning restore SYSLIB0057
                
                await _dgiiSender.Autenticar(certificate);
                
                var extension = Path.GetExtension(archivoCsv.FileName).ToLowerInvariant();
                List<CsvEcfDto> registros = (extension == ".xlsx" || extension == ".xls") 
                    ? LeerExcel(archivoCsv) 
                    : await LeerCsv(archivoCsv);

                _logger.LogInformation($"Se encontraron {registros.Count} registros.");

                int lineaNum = 0;
                foreach (var dto in registros)
                {
                    lineaNum++;
                    if (string.IsNullOrWhiteSpace(dto.Encf)) continue;

                    bool esConsumo = dto.TipoeCF == 32;
                    bool menor250k = dto.MontoTotal < 250000m;

                    if (esConsumo && menor250k)
                    {
                        reporte.Add(new { linea = lineaNum, encf = dto.Encf, estado = "SKIPPED", mensaje = "Factura Consumo < 250k." });
                        continue;
                    }

                    try
                    {
                        var ecf = MapearDtoAModeloECF(dto, rncCertificado);
                        var response = await _dgiiSender.Facturar(ecf, certificate);
                        bool success = response.Contains("trackId") || response.Contains("TrackId");
                        
                        reporte.Add(new { linea = lineaNum, encf = dto.Encf, estado = success ? "ENVIADO" : "RECHAZADO", respuesta = response });
                    }
                    catch (Exception ex)
                    {
                        reporte.Add(new { linea = lineaNum, encf = dto.Encf, estado = "ERROR_VALIDACION", error = ex.Message });
                    }
                }

                return Ok(new { mensaje = "Proceso completado", resultados = reporte });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private ECF MapearDtoAModeloECF(CsvEcfDto dto, string rncEmisorDefault)
        {
            var ecf = new ECF(); // Constructor inicializa Encabezado, DetallesItems y Subtotales
            
            ecf.Encabezado.Version = new VersionType { Value = "1.0" };
            ecf.Encabezado.IdDoc.TipoeCF = dto.TipoeCF;
            ecf.Encabezado.IdDoc.eNCF = dto.Encf;
            ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = dto.FechaVencimientoSecuencia;
            ecf.Encabezado.IdDoc.IndicadorMontoGravado = dto.IndicadorMontoGravado;
            ecf.Encabezado.IdDoc.TipoIngresosRaw = dto.TipoIngresos.ToString();
            ecf.Encabezado.IdDoc.TipoPago = dto.TipoPago == 0 ? 1 : dto.TipoPago;
            ecf.Encabezado.IdDoc.FechaLimitePago = dto.FechaLimitePago;

            ecf.Encabezado.Emisor.RNCEmisor = !string.IsNullOrEmpty(dto.RNCEmisor) ? dto.RNCEmisor : rncEmisorDefault;
            ecf.Encabezado.Emisor.RazonSocialEmisor = dto.RazonSocialEmisor;
            ecf.Encabezado.Emisor.DireccionEmisor = dto.DireccionEmisor;
            ecf.Encabezado.Emisor.FechaEmision = string.IsNullOrEmpty(dto.FechaEmision) ? DateTime.Now.ToString("dd-MM-yyyy") : dto.FechaEmision;

            ecf.Encabezado.Comprador.RNCComprador = dto.RNCComprador;
            ecf.Encabezado.Comprador.RazonSocialComprador = dto.RazonSocialComprador;

            ecf.Encabezado.Totales.MontoTotal = dto.MontoTotal;
            ecf.Encabezado.Totales.MontoGravadoTotal = dto.MontoGravadoTotal;
            ecf.Encabezado.Totales.MontoExento = dto.MontoExento;
            ecf.Encabezado.Totales.TotalITBIS = dto.TotalITBIS;

            int lineNum = 1;
            if (dto.MontoGravadoI1 > 0) {
                ecf.Encabezado.Totales.MontoGravadoI1 = dto.MontoGravadoI1;
                if(dto.ITBIS1 > 0) ecf.Encabezado.Totales.ITBIS1 = (int?)dto.ITBIS1;
                ecf.Encabezado.Totales.TotalITBIS1 = dto.TotalITBIS1 > 0 ? dto.TotalITBIS1 : dto.ITBIS1;
                ecf.DetallesItems.Item.Add(CreateItem(lineNum++, "Bienes al 18%", 2, dto.MontoGravadoI1, 1));
            }
            if (dto.MontoGravadoI2 > 0) {
                ecf.Encabezado.Totales.MontoGravadoI2 = dto.MontoGravadoI2;
                if(dto.ITBIS2 > 0) ecf.Encabezado.Totales.ITBIS2 = (int?)dto.ITBIS2;
                ecf.Encabezado.Totales.TotalITBIS2 = dto.TotalITBIS2 > 0 ? dto.TotalITBIS2 : dto.ITBIS2;
                ecf.DetallesItems.Item.Add(CreateItem(lineNum++, "Bienes al 16%", 2, dto.MontoGravadoI2, 2));
            }
            if (dto.MontoExento > 0) {
                ecf.DetallesItems.Item.Add(CreateItem(lineNum++, "Bienes Exentos", 1, dto.MontoExento, 0));
            }

            // Lista de Subtotales
            ecf.Subtotales.Subtotal.Add(new Subtotal
            {
                NumeroSubTotal = 1,
                DescripcionSubtotal = "Subtotal General",
                Orden = 1,
                SubTotalMontoGravadoTotal = dto.MontoGravadoTotal > 0 ? dto.MontoGravadoTotal : null,
                SubTotalMontoGravadoI1 = dto.MontoGravadoI1 > 0 ? dto.MontoGravadoI1 : null,
                SubTotalMontoGravadoI2 = dto.MontoGravadoI2 > 0 ? dto.MontoGravadoI2 : null,
                SubTotalExento = dto.MontoExento > 0 ? dto.MontoExento : null,
                SubTotaITBIS = dto.TotalITBIS > 0 ? dto.TotalITBIS : null,
                SubTotaITBIS1 = dto.TotalITBIS1 > 0 ? dto.TotalITBIS1 : null,
                SubTotaITBIS2 = dto.TotalITBIS2 > 0 ? dto.TotalITBIS2 : null,
                MontoSubTotal = dto.MontoTotal,
                Lineas = lineNum - 1
            });

            return ecf;
        }

        private Item CreateItem(int lineNumber, string name, int indicator, decimal amount, int taxType)
        {
            int indicadorFacturacion = 1;
            if (taxType == 0) indicadorFacturacion = 4;
            else if (taxType == 1) indicadorFacturacion = 1;
            else if (taxType == 2) indicadorFacturacion = 2;

            return new Item
            {
                NumeroLinea = lineNumber,
                NombreItem = name,
                DescripcionItem = name,
                IndicadorFacturacion = indicadorFacturacion,
                CantidadItem = 1,
                PrecioUnitarioItem = amount,
                MontoItem = amount,
                IndicadorBienoServicio = 2
            };
        }

        // Helper Methods LeerExcel y LeerCsv (Simplificados para brevedad, copiar lógica anterior)
        private List<CsvEcfDto> LeerExcel(IFormFile archivo) {
            var registros = new List<CsvEcfDto>();
            using (var stream = archivo.OpenReadStream())
            using (var workbook = new XLWorkbook(stream)) {
                var worksheet = workbook.Worksheet(1);
                var headers = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var cell in worksheet.Row(1).CellsUsed()) headers[cell.GetString().Trim().Replace("\uFEFF", "")] = cell.Address.ColumnNumber;
                int lastRow = worksheet.RangeUsed().LastRow().RowNumber();
                for (int rowNum = 2; rowNum <= lastRow; rowNum++) {
                    var row = worksheet.Row(rowNum);
                    if (!row.IsEmpty()) registros.Add(ParsearFilaExcel(row, headers));
                }
            }
            return registros;
        }

        private CsvEcfDto ParsearFilaExcel(IXLRow row, Dictionary<string, int> headers) {
            string GetVal(string key) => (headers.TryGetValue(key, out int c) || headers.TryGetValue(key.ToUpper(), out c)) ? row.Cell(c).GetString().Trim() : "";
            decimal GetDec(string key) {
                if (headers.TryGetValue(key, out int c) || headers.TryGetValue(key.ToUpper(), out c)) {
                    var s = row.Cell(c).GetString().Trim();
                    return (string.IsNullOrEmpty(s) || s == "#e") ? 0m : (decimal.TryParse(s.Replace("$","").Replace(",",""), out decimal d) ? d : 0m);
                }
                return 0m;
            }
            int GetInt(string key) {
                if (headers.TryGetValue(key, out int c) || headers.TryGetValue(key.ToUpper(), out c)) {
                    var s = row.Cell(c).GetString().Trim();
                    return (string.IsNullOrEmpty(s) || s == "#e") ? 0 : (int.TryParse(s, out int i) ? i : 0);
                }
                return 0;
            }
            return new CsvEcfDto {
                Encf = GetVal("ENCF"), TipoeCF = GetInt("TipoeCF"), FechaVencimientoSecuencia = GetVal("FechaVencimientoSecuencia"),
                IndicadorMontoGravado = GetInt("IndicadorMontoGravado"), TipoIngresos = GetInt("TipoIngresos"), TipoPago = GetInt("TipoPago"),
                FechaLimitePago = GetVal("FechaLimitePago"), RNCEmisor = GetVal("RNCEmisor"), RazonSocialEmisor = GetVal("RazonSocialEmisor"),
                DireccionEmisor = GetVal("DireccionEmisor"), FechaEmision = GetVal("FechaEmision"), RNCComprador = GetVal("RNCComprador"),
                RazonSocialComprador = GetVal("RazonSocialComprador"), MontoTotal = GetDec("MontoTotal"), MontoGravadoTotal = GetDec("MontoGravadoTotal"),
                MontoGravadoI1 = GetDec("MontoGravadoI1"), MontoGravadoI2 = GetDec("MontoGravadoI2"), MontoExento = GetDec("MontoExento"),
                TotalITBIS = GetDec("TotalITBIS"), ITBIS1 = GetDec("ITBIS1"), ITBIS2 = GetDec("ITBIS2"), TotalITBIS1 = GetDec("TotalITBIS1"), TotalITBIS2 = GetDec("TotalITBIS2")
            };
        }

        private async Task<List<CsvEcfDto>> LeerCsv(IFormFile archivo) { return new List<CsvEcfDto>(); } // Placeholder, implementar si necesario
    }
}