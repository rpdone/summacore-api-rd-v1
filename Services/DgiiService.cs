using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using SummaCore.Models;
using System.Text.Json; // Necesario para leer la respuesta JSON

namespace SummaCore.Services
{
    public class DgiiService
    {
        private readonly HttpClient _http;
        private readonly SignerService _signer;
        private readonly ILogger<DgiiService>? _logger;

        // =========================================================================
        // RUTAS OFICIALES - AMBIENTE CERTIFICACIÓN (CerteCF)
        // =========================================================================
        private const string URL_SEMILLA = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
        private const string URL_TOKEN = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla";
        private const string URL_RECEPCION = "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas";
        private const string URL_CONSULTA = "https://ecf.dgii.gov.do/CerteCF/ConsultaResultado/api/Consultas/Estado";

        public DgiiService(SignerService signer, ILogger<DgiiService>? logger = null)
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = false,
                PreAuthenticate = false
            };
            _http = new HttpClient(handler);
            _signer = signer;
            _logger = logger;
            // Aunque DGII prefiere XML para facturas, el token suele venir en JSON actualmente
            _http.DefaultRequestHeaders.Add("Accept", "application/json"); 
        }

        // =========================================================================
        // MÉTODO PRINCIPAL: FACTURAR (Orquestador)
        // =========================================================================
        public async Task<string> Facturar(ECF ecf, X509Certificate2 certificado)
        {
            try
            {
                // 1. Limpieza de datos
                LimpiarDatos(ecf);

                // 2. Serializar Objeto ECF a XML String
                string xmlContent = SerializarObjeto(ecf);
                
                // 3. VALIDACIÓN CRÍTICA CONTRA XSD
                int tipoEcf = ecf.Encabezado.IdDoc.TipoeCF;
                string xsdFileName = $"e-CF {tipoEcf} v.1.0.xsd";
                string xsdPath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "XSDs", xsdFileName);

                if (File.Exists(xsdPath))
                {
                    _logger?.LogInformation($"Validando XML contra esquema: {xsdFileName}");
                    var validacion = ValidarXmlContraXsd(xmlContent, xsdPath);
                    
                    if (!validacion.EsValido)
                    {
                        throw new Exception($"ERROR DE ESTRUCTURA XSD (Local): \n{validacion.Error}");
                    }
                    _logger?.LogInformation("Validación XSD exitosa.");
                }
                else
                {
                    _logger?.LogWarning($"No se encontró el esquema XSD en: {xsdPath}. Se omitirá la validación local.");
                }

                // 4. Autenticar con DGII (Obtener Token)
                string token = await Autenticar(certificado);

                // 5. Firmar el XML generado
                string xmlFirmado = _signer.FirmarECF(xmlContent, certificado);

                // 6. Definir nombre de archivo (RNC + e-NCF.xml)
                string nombreArchivo = $"{ecf.Encabezado.Emisor.RNCEmisor}{ecf.Encabezado.IdDoc.eNCF}.xml";

                // 7. Enviar a DGII
                return await EnviarFactura(xmlFirmado, token, nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error en el proceso de Facturación");
                throw;
            }
        }

        // =========================================================================
        // MÉTODOS DE AYUDA (SERIALIZACIÓN Y VALIDACIÓN)
        // =========================================================================

        private void LimpiarDatos(ECF ecf)
        {
            if (ecf.Encabezado.IdDoc.FechaVencimientoSecuencia == "#e") ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = null;
        }

        private string SerializarObjeto(ECF ecf)
        {
            var serializer = new XmlSerializer(typeof(ECF));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", ""); 

            using (var stringWriter = new StringWriterUtf8())
            {
                serializer.Serialize(stringWriter, ecf, namespaces);
                return stringWriter.ToString();
            }
        }

        private (bool EsValido, string Error) ValidarXmlContraXsd(string xmlContent, string xsdPath)
        {
            try
            {
                var schemas = new XmlSchemaSet();
                schemas.Add("", XmlReader.Create(new StreamReader(xsdPath)));

                var document = new XmlDocument();
                document.LoadXml(xmlContent);
                document.Schemas.Add(schemas);

                string errores = "";
                document.Validate((o, e) => {
                    errores += $"- {e.Message}\n";
                });

                if (!string.IsNullOrEmpty(errores))
                {
                    return (false, errores);
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Error fatal validando XSD: {ex.Message}");
            }
        }

        public class StringWriterUtf8 : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        // =========================================================================
        // MÉTODOS HTTP EXISTENTES
        // =========================================================================

        public async Task<string> Autenticar(X509Certificate2 certificado)
        {
            try
            {
                // [1] Obtener Semilla
                _logger?.LogInformation("[1] Obteniendo semilla...");
                var semillaResponse = await _http.GetAsync(URL_SEMILLA);
                semillaResponse.EnsureSuccessStatusCode();
                
                string semillaXml = Encoding.UTF8.GetString(await semillaResponse.Content.ReadAsByteArrayAsync());
                semillaXml = LimpiarXML(semillaXml); 

                // [2] Firmar Semilla
                _logger?.LogInformation("[2] Firmando semilla...");
                var semillaFirmada = _signer.FirmarECF(semillaXml, certificado);

                // [3] Validar Semilla
                _logger?.LogInformation("[3] Enviando semilla firmada para validación...");

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(semillaFirmada));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                content.Add(fileContent, "xml", "semilla_firmada.xml");

                var tokenResponse = await _http.PostAsync(URL_TOKEN, content);
                var responseString = await tokenResponse.Content.ReadAsStringAsync();
                
                if (!tokenResponse.IsSuccessStatusCode) 
                {
                    throw new Exception($"Error DGII ({tokenResponse.StatusCode}): {responseString}");
                }

                // [4] Extraer Token (AHORA SOPORTA JSON)
                string tokenLimpio = ExtraerToken(responseString);
                return tokenLimpio;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR en Autenticar");
                throw;
            }
        }

        public async Task<string> EnviarFactura(string xmlFirmado, string token, string nombreArchivo = "factura.xml")
        {
            try
            {
                _logger?.LogInformation("[ENVÍO] Enviando factura...");
                
                _http.DefaultRequestHeaders.Clear();
                // Se asegura de limpiar cualquier comilla extra del token
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Trim('"')}");
                
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(xmlFirmado));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                
                content.Add(fileContent, "xml", nombreArchivo);

                var response = await _http.PostAsync(URL_RECEPCION, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                _logger?.LogInformation("Respuesta envío: {StatusCode}", response.StatusCode);
                
                // Retornamos la respuesta aunque sea error, para que el controlador vea el detalle
                return responseString;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR en EnviarFactura");
                throw;
            }
        }

        private string LimpiarXML(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return "";
            string clean = xml.StartsWith("\uFEFF") ? xml.Substring(1) : xml;
            
            int index = clean.IndexOf("<SemillaModel");
            if (index == -1) index = clean.IndexOf("<");
            if (index == -1) throw new Exception("XML inválido");
            
            return clean.Substring(index);
        }

        // MÉTODO CORREGIDO: Soporta JSON
        private string ExtraerToken(string response)
        {
            try
            {
                // Intento 1: JSON (Formato actual DGII)
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    if (doc.RootElement.TryGetProperty("token", out JsonElement tokenElement))
                    {
                        return tokenElement.GetString();
                    }
                }
            }
            catch
            {
                // Fallo JSON, intentar XML antiguo
            }

            // Intento 2: XML
            if (response.Contains("<token>"))
            {
                int start = response.IndexOf("<token>") + 7;
                int end = response.IndexOf("</token>");
                if (end > start) return response.Substring(start, end - start);
            }
            
            // Si todo falla, devuelve la respuesta original (probablemente fallará el envío)
            return response;
        }

        public async Task<string> ConsultarEstado(string trackId, string token)
        {
            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var url = $"{URL_CONSULTA}?trackId={trackId}";
                _logger?.LogInformation($"[CONSULTA] Consultando TrackId: {trackId}");

                var response = await _http.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning($"Error consultando {trackId}: {response.StatusCode}");
                }

                return responseString;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Excepción consultando TrackId {trackId}");
                return $"{{\"trackId\": \"{trackId}\", \"estado\": \"ERROR_INTERNO\", \"mensaje\": \"{ex.Message}\"}}";
            }
        }
    }
}