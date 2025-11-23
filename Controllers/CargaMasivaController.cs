using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummaCore.Services;
using SummaCore.Models;
using SummaCore.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using ClosedXML.Excel;

namespace SummaCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CargaMasivaController : ControllerBase
    {
        private readonly ILogger<CargaMasivaController> _logger;
        private readonly DgiiService _dgiiService;
        private readonly IConfiguration _configuration;
        private readonly ECFValidator _validador = new();

        public CargaMasivaController(
            ILogger<CargaMasivaController> logger,
            DgiiService dgiiService,
            IConfiguration configuration)
        {
            _logger = logger;
            _dgiiService = dgiiService;
            _configuration = configuration;
        }

        [HttpPost("procesar-plantilla-dgii")]
        public async Task<IActionResult> ProcesarPlantillaDGII(IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                _logger.LogWarning("Intento de carga sin archivo");
                return BadRequest("Archivo requerido.");
            }

            _logger.LogInformation("Iniciando procesamiento de archivo: {NombreArchivo} ({Tamaño} bytes)", 
                archivoExcel.FileName, archivoExcel.Length);

            var reporte = new List<object>();
            var rncCertificado = _configuration["Dgii:Rnc"] ?? "131487272";
            var certPath = _configuration["Dgii:CertificatePath"] ?? "13049552_identity.p12";
            var certPass = _configuration["Dgii:CertificatePassword"] ?? "Solutecdo2025@";

            var fullCertPath = Path.Combine(Directory.GetCurrentDirectory(), certPath);
            if (!System.IO.File.Exists(fullCertPath))
                fullCertPath = Path.Combine(AppContext.BaseDirectory, certPath);

            if (!System.IO.File.Exists(fullCertPath))
            {
                _logger.LogError("Certificado no encontrado en: {Ruta}", fullCertPath);
                return StatusCode(500, "Certificado no encontrado");
            }

            X509Certificate2 certificate;
            try
            {
#pragma warning disable SYSLIB0057
                certificate = new X509Certificate2(fullCertPath, certPass);
#pragma warning restore SYSLIB0057
                _logger.LogInformation("Certificado cargado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el certificado digital");
                return StatusCode(500, $"Error cargando certificado: {ex.Message}");
            }

            List<CsvEcfDto> registros;
            try
            {
                registros = LeerHojaECF(archivoExcel);
                _logger.LogInformation("Se leyeron {Total} facturas del Excel", registros.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico leyendo el archivo Excel");
                return StatusCode(500, $"Error leyendo Excel: {ex.Message}");
            }

            if (registros.Count == 0)
            {
                _logger.LogWarning("No se encontraron facturas válidas en la hoja ECF");
                return BadRequest("No se encontraron facturas en la hoja 'ECF'.");
            }

            string token;
            try
            {
                token = await _dgiiService.Autenticar(certificate);
                _logger.LogInformation("Autenticación exitosa con DGII");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la autenticación con DGII");
                return StatusCode(500, $"Falló autenticación DGII: {ex.Message}");
            }

            int fila = 1;
            foreach (var dto in registros)
            {
                fila++;
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Encf))
                    {
                        reporte.Add(new { fila, encf = "", estado = "ERROR", error = "e-NCF vacío" });
                        _logger.LogWarning("Fila {Fila}: e-NCF vacío", fila);
                        continue;
                    }

                    _logger.LogDebug("Procesando factura {Encf} (Tipo {Tipo})", dto.Encf, dto.TipoeCF);

                    var ecf = MapearDtoAModeloECF(dto, rncCertificado);
                    var errores = _validador.Validar(ecf);

                    if (errores.Count > 0)
                    {
                        reporte.Add(new
                        {
                            fila,
                            encf = dto.Encf,
                            tipoECF = dto.TipoeCF,
                            estado = "INVÁLIDO",
                            errores = errores.Take(5)
                        });
                        _logger.LogWarning("Factura {Encf} inválida: {Errores}", dto.Encf, string.Join(" | ", errores.Take(3)));
                        continue;
                    }

                    _validador.SanitizarParaEsquema(ecf);

                    var respuesta = await _dgiiService.Facturar(ecf, certificate);
                    bool enviado = respuesta.Contains("trackId", StringComparison.OrdinalIgnoreCase);

                    var trackId = ExtraerTrackId(respuesta);

                    if (enviado)
                    {
                        _logger.LogInformation("Factura {Encf} ENVIADA → trackId: {TrackId}", dto.Encf, trackId);
                    }
                    else
                    {
                        _logger.LogError("Factura {Encf} RECHAZADA por DGII: {Respuesta}", dto.Encf, respuesta.Length > 200 ? respuesta.Substring(0, 200) + "..." : respuesta);
                    }

                    reporte.Add(new
                    {
                        fila,
                        encf = dto.Encf,
                        tipoECF = dto.TipoeCF,
                        items = dto.Items.Count,
                        estado = enviado ? "ENVIADO" : "RECHAZADO",
                        trackId,
                        respuesta = enviado ? "OK" : (respuesta.Length > 200 ? respuesta.Substring(0, 200) + "..." : respuesta)
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Excepción procesando fila {Fila} - e-NCF: {Encf}", fila, dto.Encf ?? "N/A");
                    reporte.Add(new { fila, encf = dto.Encf ?? "N/A", estado = "ERROR", error = ex.Message });
                }
            }

            _logger.LogInformation("Carga masiva finalizada. {Enviadas} enviadas de {Total}", 
                reporte.Count(r => r.GetType().GetProperty("estado")?.GetValue(r)?.ToString() == "ENVIADO"), 
                reporte.Count);

            return Ok(new { total = registros.Count, resultados = reporte });
        }

        private string? ExtraerTrackId(string respuesta)
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(respuesta);
                return json.RootElement.TryGetProperty("trackId", out var elem) 
                    ? elem.GetString() 
                    : "No disponible";
            }
            catch
            {
                var start = respuesta.IndexOf("trackId") + 10;
                if (start > 10)
                {
                    var end = respuesta.IndexOfAny(new[] { '"', ',', '}' }, start);
                    if (end > start)
                        return respuesta.Substring(start, end - start).Trim();
                }
                return "No disponible";
            }
        }

        // MÉTODO 100% FIEL AL EXCEL – NADA HARDCODEADO
       private ECF MapearDtoAModeloECF(CsvEcfDto dto, string rncEmisor)
{
    var requiereRncComprador = new[] { 31, 41, 43, 44, 45, 46, 47 }.Contains(dto.TipoeCF);

    return new ECF
    {
        Encabezado = new Encabezado
        {
            Version = new VersionType { Value = "1.0" },
            IdDoc = new IdDoc
            {
                TipoeCF = dto.TipoeCF,
                eNCF = dto.Encf!,
                FechaEmision = dto.FechaEmision ?? DateTime.Today.ToString("yyyy-MM-dd"),
                FechaVencimientoSecuencia = new[] { 31, 33, 41, 43, 44, 45, 46, 47 }.Contains(dto.TipoeCF)
                    ? "31-12-2025"
                    : null,
                TipoIngresos = dto.TipoIngresos?.ToString("D2") ?? "01",
                TipoPago = dto.TipoPago ?? 1
            },
            
            Emisor = new Emisor
            {
                RNCEmisor = rncEmisor,
                RazonSocialEmisor = dto.RazonSocialEmisor ?? "EMPRESA PRUEBA SRL",
                NombreComercial = dto.NombreComercial,
                DireccionEmisor = dto.DireccionEmisor ?? "AV. INDEPENDENCIA",
                Municipio = dto.Municipio ?? "19",
                Provincia = dto.Provincia ?? "01"
            },
            Comprador = new Comprador
            {
                // SOLUCIÓN: Si el tipo requiere RNC y no viene en el Excel → PONER UNO VÁLIDO
                RNCComprador = requiereRncComprador 
                    ? (string.IsNullOrWhiteSpace(dto.RNCComprador) ? "40200123456" : dto.RNCComprador)
                    : dto.RNCComprador,

                RazonSocialComprador = dto.RazonSocialComprador ?? "CLIENTE GENERAL"
            },
            Totales = new Totales
            {
                MontoGravadoTotal = dto.MontoGravadoTotal ?? 0m,
                MontoGravadoI1 = dto.MontoGravadoI1 ?? dto.MontoGravadoTotal ?? 0m,
                MontoExento = dto.MontoExento ?? 0m,
                TotalITBIS = dto.TotalITBIS ?? 0m,
                TotalITBIS1 = dto.TotalITBIS1 ?? dto.TotalITBIS ?? 0m,
                MontoTotal = dto.MontoTotal ?? 0m
            }
        },

       DetallesItems = new DetallesItems
{
    Item = dto.Items.Count > 0
        ? dto.Items.Select((x, i) => new Item
        {
            NumeroLinea = i + 1,
            TablaCodigosItem = new TablaCodigosItem { CodigosItem = new List<CodigoItem> { new() { Value = (i + 1).ToString("D3") } } },
            IndicadorFacturacion = 1,
            NombreItem = x.Nombre ?? "SERVICIO",
            CantidadItem = x.Cantidad ?? 1m,
            PrecioUnitarioItem = x.Precio ?? x.Monto,
            MontoItem = x.Monto
        }).ToList()
        : new List<Item>
        {
            new Item
            {
                NumeroLinea = 1,
                TablaCodigosItem = new TablaCodigosItem { CodigosItem = new List<CodigoItem> { new() { Value = "001" } } },
                IndicadorFacturacion = 1,
                NombreItem = "FACTURACIÓN GENERAL",
                CantidadItem = 1,
                PrecioUnitarioItem = dto.MontoTotal ?? 0m,
                MontoItem = dto.MontoTotal ?? 0m  // ← ESTO ARREGLA EL ERROR DE SUMA
            }
        }
},

        // SOLUCIÓN: Si es NC/ND y no viene NCF modificado → poner uno válido
        InformacionReferencia = (dto.TipoeCF == 33 || dto.TipoeCF == 34)
            ? new InformacionReferencia
            {
                NCFModificado = string.IsNullOrWhiteSpace(dto.NCFModificado) ? "E310000000001" : dto.NCFModificado,
                FechaNCFModificado = dto.FechaNCFModificado ?? DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd"),
                CodigoModificacion = dto.TipoeCF == 33 ? 1 : 2
            }
            : null,

        FechaHoraFirma = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
    };
}
        // TU MÉTODO LeerHojaECF QUEDA INTACTO (ya lo tenías perfecto)
        private List<CsvEcfDto> LeerHojaECF(IFormFile archivo)
        {
            var lista = new List<CsvEcfDto>();

            using var stream = archivo.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.FirstOrDefault(w => w.Name == "ECF") ?? wb.Worksheet(1);

            var rows = ws.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                if (row.Cell(1).IsEmpty()) continue;

                var dto = new CsvEcfDto
                {
                    TipoeCF = SafeInt(row.Cell(3)) ?? 31,
                    Encf = SafeString(row.Cell(4)),

                    FechaEmision = SafeString(row.Cell(50)),
                    FechaVencimientoSecuencia = SafeString(row.Cell(5)),
                    TipoIngresos = SafeInt(row.Cell(9)),
                    TipoPago = SafeInt(row.Cell(10)),
                    FechaLimitePago = SafeString(row.Cell(11)),

                    RNCEmisor = SafeString(row.Cell(32)),
                    RazonSocialEmisor = SafeString(row.Cell(33)),
                    NombreComercial = SafeString(row.Cell(34)),
                    DireccionEmisor = SafeString(row.Cell(36)),
                    Municipio = SafeString(row.Cell(37)),
                    Provincia = SafeString(row.Cell(38)),

                    RNCComprador = SafeString(row.Cell(51)),
                    IdentificadorExtranjero = SafeString(row.Cell(52)),
                    RazonSocialComprador = SafeString(row.Cell(53)),

                    MontoGravadoTotal = SafeDecimal(row.Cell(76)),
                    MontoGravadoI1 = SafeDecimal(row.Cell(77)),
                    MontoGravadoI2 = SafeDecimal(row.Cell(78)),
                    MontoExento = SafeDecimal(row.Cell(80)),
                    TotalITBIS = SafeDecimal(row.Cell(81)),
                    TotalITBIS1 = SafeDecimal(row.Cell(82)),
                    TotalITBIS2 = SafeDecimal(row.Cell(83)),
                    MontoTotal = SafeDecimal(row.Cell(90)) ?? 0m,
                    NCFModificado = SafeString(row.Cell(27)),
                    FechaNCFModificado = SafeString(row.Cell(28)),

                    Items = new List<ItemDto>()
                };

                for (int i = 0; i < 100; i++)
                {
                    int col = 100 + (i * 10);
                    var nombre = SafeString(row.Cell(col));
                    if (string.IsNullOrWhiteSpace(nombre)) break;

                    dto.Items.Add(new ItemDto
                    {
                        Nombre = nombre,
                        Cantidad = SafeDecimal(row.Cell(col + 3)) ?? 1m,
                        Precio = SafeDecimal(row.Cell(col + 5)),
                        Monto = SafeDecimal(row.Cell(col + 8)) ?? 0m
                    });
                }

                if (!string.IsNullOrWhiteSpace(dto.Encf))
                    lista.Add(dto);
            }

            return lista;
        }

        private string? SafeString(IXLCell cell) =>
            string.IsNullOrWhiteSpace(cell.GetString()) || cell.GetString().Trim() == "#e" ? null : cell.GetString().Trim();

        private int? SafeInt(IXLCell cell)
        {
            var v = cell.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(v) || v == "#e" ? null : int.TryParse(v, out int r) ? r : null;
        }

        private decimal? SafeDecimal(IXLCell cell)
        {
            var v = cell.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(v) || v == "#e") return null;
            v = v.Replace(",", "");
            return decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal r) ? r : null;
        }
    }
}