using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;
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
        /// Endpoint de diagnóstico para ver el XML firmado que se envía
        /// GET: /api/pruebas/diagnostico-firma
        /// </summary>
        [HttpGet("diagnostico-firma")]
        public async Task<IActionResult> DiagnosticoFirma()
        {
            try
            {
                _logger.LogInformation("=== DIAGNÓSTICO DE FIRMA ===");
                
                // 1. Obtener semilla de DGII
                var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("Accept", "application/xml");
                
                var urlSemilla = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
                var response = await http.GetAsync(urlSemilla);
                var semillaRaw = await response.Content.ReadAsStringAsync();
                
                // 2. Limpiar semilla (extraer solo <SemillaModel>)
                int indexSemilla = semillaRaw.IndexOf("<SemillaModel");
                string semillaLimpia = indexSemilla >= 0 ? semillaRaw.Substring(indexSemilla) : semillaRaw;
                semillaLimpia = semillaLimpia.Trim();
                
                // 3. Cargar certificado
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "13049552_identity.p12");
                if (!System.IO.File.Exists(rutaCert))
                {
                    return BadRequest(new { error = "Certificado no encontrado", ruta = rutaCert });
                }
                
                var passCert = "Solutecdo2025@";
                var certificado = new X509Certificate2(rutaCert, passCert);
                
                // 4. Firmar
                var xmlFirmado = _signer.FirmarECF(semillaLimpia, certificado);
                
                // 5. Extraer info de la firma para diagnóstico
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlFirmado);
                
                var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                
                var canonMethod = doc.SelectSingleNode("//ds:CanonicalizationMethod/@Algorithm", nsmgr)?.Value;
                var sigMethod = doc.SelectSingleNode("//ds:SignatureMethod/@Algorithm", nsmgr)?.Value;
                var digestMethod = doc.SelectSingleNode("//ds:DigestMethod/@Algorithm", nsmgr)?.Value;
                var transforms = doc.SelectNodes("//ds:Transform/@Algorithm", nsmgr);
                
                var transformsList = new System.Collections.Generic.List<string>();
                if (transforms != null)
                {
                    foreach (System.Xml.XmlNode t in transforms)
                    {
                        transformsList.Add(t.Value ?? "null");
                    }
                }
                
                return Ok(new {
                    paso1_semillaOriginal = semillaRaw.Substring(0, Math.Min(200, semillaRaw.Length)) + "...",
                    paso2_semillaLimpia = semillaLimpia.Substring(0, Math.Min(200, semillaLimpia.Length)) + "...",
                    paso3_certificado = new {
                        subject = certificado.Subject,
                        thumbprint = certificado.Thumbprint,
                        tieneClavePrivada = certificado.HasPrivateKey
                    },
                    paso4_firmaAnalisis = new {
                        canonicalizationMethod = canonMethod,
                        signatureMethod = sigMethod,
                        digestMethod = digestMethod,
                        transforms = transformsList,
                        esperado_canonicalization = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315",
                        esperado_signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                        esperado_digest = "http://www.w3.org/2001/04/xmlenc#sha256"
                    },
                    paso5_xmlFirmadoCompleto = xmlFirmado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en diagnóstico firma: {Message}", ex.Message);
                return BadRequest(new { 
                    error = ex.Message, 
                    tipo = ex.GetType().Name,
                    stack = ex.StackTrace?.Split('\n').Take(5).ToArray()
                });
            }
        }

        /// <summary>
        /// Endpoint de diagnóstico para ver qué devuelve la DGII
        /// GET: /api/pruebas/diagnostico-semilla
        /// </summary>
        [HttpGet("diagnostico-semilla")]
        public async Task<IActionResult> DiagnosticoSemilla()
        {
            try
            {
                _logger.LogInformation("=== DIAGNÓSTICO DE SEMILLA ===");
                
                var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("Accept", "application/xml");
                
                var url = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
                _logger.LogInformation("Llamando a: {Url}", url);
                
                var response = await http.GetAsync(url);
                
                // Leer como bytes crudos
                var bytesRaw = await response.Content.ReadAsByteArrayAsync();
                
                // Convertir a hex para ver caracteres invisibles
                var hexString = BitConverter.ToString(bytesRaw.Take(100).ToArray());
                
                // Convertir a string UTF8
                var stringUtf8 = System.Text.Encoding.UTF8.GetString(bytesRaw);
                
                // Detectar BOM
                bool tieneBOM = bytesRaw.Length >= 3 && 
                                bytesRaw[0] == 0xEF && 
                                bytesRaw[1] == 0xBB && 
                                bytesRaw[2] == 0xBF;
                
                // Detectar si empieza con caracteres raros
                var primeros20Chars = stringUtf8.Length > 20 ? stringUtf8.Substring(0, 20) : stringUtf8;
                var primeros20CharCodes = string.Join(",", primeros20Chars.Select(c => ((int)c).ToString()));
                
                // Intentar encontrar el inicio del XML
                int indexSemilla = stringUtf8.IndexOf("<SemillaModel");
                int indexXmlDecl = stringUtf8.IndexOf("<?xml");
                
                _logger.LogInformation("Status: {Status}", response.StatusCode);
                _logger.LogInformation("Content-Type: {ContentType}", response.Content.Headers.ContentType?.ToString());
                _logger.LogInformation("Bytes totales: {Length}", bytesRaw.Length);
                _logger.LogInformation("Tiene BOM: {TieneBOM}", tieneBOM);
                _logger.LogInformation("Hex primeros 100 bytes: {Hex}", hexString);
                
                return Ok(new {
                    status = response.StatusCode.ToString(),
                    contentType = response.Content.Headers.ContentType?.ToString(),
                    bytesTotales = bytesRaw.Length,
                    tieneBOM = tieneBOM,
                    hexPrimeros100Bytes = hexString,
                    primeros20Caracteres = primeros20Chars,
                    codigosCaracteres = primeros20CharCodes,
                    indexSemillaModel = indexSemilla,
                    indexXmlDeclaration = indexXmlDecl,
                    contenidoCompleto = stringUtf8,
                    contenidoLimpio = indexSemilla >= 0 ? stringUtf8.Substring(indexSemilla) : "NO ENCONTRADO"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en diagnóstico: {Message}", ex.Message);
                return BadRequest(new { 
                    error = ex.Message, 
                    tipo = ex.GetType().Name,
                    stack = ex.StackTrace?.Split('\n').Take(5).ToArray()
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