using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SummaCore.Services;
using SummaCore.Models;

namespace SummaCore.Controllers
{
    [Route("api/pruebas")]
    [ApiController]
    public class PruebasController : ControllerBase
    {
        private readonly ILogger<PruebasController> _logger;
        private readonly SignerService _signer;
        private readonly DgiiService _dgiiSender;

        public PruebasController(ILogger<PruebasController> logger, ILogger<DgiiService> dgiiLogger)
        {
            _logger = logger;
            _signer = new SignerService();
            _dgiiSender = new DgiiService(_signer, dgiiLogger);
        }

        /// <summary>
        /// Endpoint de prueba para autenticación con DGII
        /// GET: /api/pruebas/test-autenticacion
        /// </summary>
        [HttpGet("test-autenticacion")]
        public async Task<IActionResult> TestAutenticacion()
        {
            try 
            {
                _logger.LogInformation("=== INICIANDO TEST DE AUTENTICACIÓN ===");
                
                // 1. Cargar Certificado
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");
                
                if (!System.IO.File.Exists(rutaCert))
                {
                    _logger.LogError("Certificado no encontrado en: {RutaCert}", rutaCert);
                    return BadRequest(new { 
                        error = "Certificado no encontrado", 
                        ruta = rutaCert,
                        directorio = Directory.GetCurrentDirectory()
                    });
                }
                
                _logger.LogInformation("Cargando certificado desde: {RutaCert}", rutaCert);
                
                var passCert = "Solutecdo2025@";
                var certificado = new X509Certificate2(rutaCert, passCert);
                
                _logger.LogInformation("Certificado cargado: {Subject}", certificado.Subject);
                _logger.LogInformation("Válido desde: {NotBefore} hasta: {NotAfter}", certificado.NotBefore, certificado.NotAfter);
                
                // 2. Autenticarse con DGII
                _logger.LogInformation("=== AUTENTICANDO CON DGII ===");
                var token = await _dgiiSender.Autenticar(certificado);
                
                _logger.LogInformation("=== AUTENTICACIÓN EXITOSA ===");
                
                return Ok(new { 
                    mensaje = "Autenticación exitosa",
                    token = token.Substring(0, Math.Min(50, token.Length)) + "...",
                    certificado = new {
                        subject = certificado.Subject,
                        issuer = certificado.Issuer,
                        validoDesde = certificado.NotBefore,
                        validoHasta = certificado.NotAfter
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR en TestAutenticacion: {ErrorMessage}", ex.Message);
                
                return BadRequest(new { 
                    error = ex.Message, 
                    tipo = ex.GetType().Name,
                    stack = ex.StackTrace?.Split('\n').Take(5).ToArray()
                });
            }
        }

        /// <summary>
        /// Endpoint completo: Autenticación + Envío de Factura
        /// GET: /api/pruebas/ejecutar-paso2
        /// </summary>
        [HttpGet("ejecutar-paso2")]
        public async Task<IActionResult> EjecutarPaso2()
        {
            try 
            {
                _logger.LogInformation("=== INICIANDO PROCESO COMPLETO ===");
                
                // 1. Cargar Certificado
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");
                
                if (!System.IO.File.Exists(rutaCert))
                {
                    _logger.LogError("Certificado no encontrado en: {RutaCert}", rutaCert);
                    return BadRequest(new { 
                        error = "Certificado no encontrado", 
                        ruta = rutaCert 
                    });
                }
                
                var passCert = "Solutecdo2025@";
                var certificado = new X509Certificate2(rutaCert, passCert);
                
                _logger.LogInformation("Certificado: {Subject}", certificado.Subject);

                // 2. Autenticarse con DGII
                _logger.LogInformation("[PASO 1] Autenticando...");
                var token = await _dgiiSender.Autenticar(certificado);
                _logger.LogInformation("✓ Token obtenido");

                // 3. Generar XML de Factura (Ejemplo basado en tu CSV fila 4)
                _logger.LogInformation("[PASO 2] Generando factura...");
                
                var factura = new ECF
                {
                    Encabezado = new Encabezado
                    {
                        IdDoc = new IdDoc 
                        { 
                            TipoeCF = 31, // Crédito Fiscal
                            eNCF = "E310000000001", // Secuencia de prueba
                            FechaVencimientoSecuencia = "31-12-2025",
                            IndicadorMontoGravado = 0,
                            TipoIngresos = 1
                        },
                        Emisor = new Emisor 
                        { 
                            RNCEmisor = "131487272", // REEMPLAZA CON TU RNC
                            RazonSocialEmisor = "TU EMPRESA S.R.L.",
                            FechaEmision = DateTime.Now.ToString("dd-MM-yyyy")
                        },
                        Comprador = new Comprador
                        {
                            RNCComprador = "101796361", // Del CSV fila 4
                            RazonSocialComprador = "CLIENTE DE PRUEBA"
                        },
                        Totales = new Totales
                        {
                            MontoTotal = 1500.00m // Del CSV fila 4
                        }
                    }
                };

                // 4. Serializar a XML
                var stringWriter = new StringWriter();
                var serializer = new XmlSerializer(typeof(ECF));
                serializer.Serialize(stringWriter, factura);
                string xmlSinFirma = stringWriter.ToString();
                
                // Guardar XML sin firma para debug
                await System.IO.File.WriteAllTextAsync("factura_sin_firma.xml", xmlSinFirma);
                _logger.LogInformation("✓ Factura generada: factura_sin_firma.xml");

                // 5. Firmar
                _logger.LogInformation("[PASO 3] Firmando factura...");
                string xmlFirmado = _signer.FirmarECF(xmlSinFirma, certificado);
                
                await System.IO.File.WriteAllTextAsync("factura_firmada.xml", xmlFirmado);
                _logger.LogInformation("✓ Factura firmada: factura_firmada.xml");

                // 6. Enviar
                _logger.LogInformation("[PASO 4] Enviando a DGII...");
                string resultado = await _dgiiSender.EnviarFactura(xmlFirmado, token);
                
                _logger.LogInformation("=== PROCESO COMPLETADO ===");

                return Ok(new { 
                    mensaje = "Proceso completado",
                    token = token.Substring(0, Math.Min(30, token.Length)) + "...",
                    respuestaDgii = resultado,
                    archivosGenerados = new[] {
                        "factura_sin_firma.xml",
                        "factura_firmada.xml",
                        "semilla_firmada_debug.xml"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR en EjecutarPaso2: {ErrorMessage}", ex.Message);
                
                return BadRequest(new { 
                    error = ex.Message,
                    tipo = ex.GetType().Name,
                    detalles = ex.InnerException?.Message,
                    stack = ex.StackTrace?.Split('\n').Take(10).ToArray()
                });
            }
        }

        /// <summary>
        /// Endpoint para probar solo la firma de un XML simple
        /// GET: /api/pruebas/test-firma
        /// </summary>
        [HttpGet("test-firma")]
        public IActionResult TestFirma()
        {
            try
            {
                _logger.LogInformation("=== TEST DE FIRMA ===");
                
                // XML simple de prueba
                string xmlPrueba = @"<SemillaModel>
  <valor>ABC123</valor>
  <fecha>2025-11-19T22:00:00</fecha>
</SemillaModel>";

                // Cargar certificado
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");
                var passCert = "Solutecdo2025@";
                var certificado = new X509Certificate2(rutaCert, passCert);
                
                // Firmar
                var xmlFirmado = _signer.FirmarECF(xmlPrueba, certificado);
                
                // Guardar
                System.IO.File.WriteAllText("test_firma.xml", xmlFirmado);
                
                _logger.LogInformation("✓ Archivo firmado guardado: test_firma.xml");
                
                return Ok(new {
                    mensaje = "Firma exitosa",
                    archivo = "test_firma.xml",
                    tamano = xmlFirmado.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR en TestFirma: {ErrorMessage}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}