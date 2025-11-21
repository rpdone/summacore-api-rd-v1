using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using System.Net.Http;


namespace SummaCore.Services
{
    public class DgiiService
    {
        private readonly HttpClient _http;
        private readonly SignerService _signer;
        private readonly ILogger<DgiiService>? _logger;
        
        // URLs AMBIENTE DE CERTIFICACIÓN (CerteCF)
        private const string URL_SEMILLA = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
        private const string URL_TOKEN = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla";
        private const string URL_RECEPCION = "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/eCF";

        public DgiiService(SignerService signer, ILogger<DgiiService>? logger = null)
        {
            _http = new HttpClient();
            _signer = signer;
            _logger = logger;
            
            // Agregar headers por defecto
            _http.DefaultRequestHeaders.Add("Accept", "application/xml");
        }

        public async Task<string> Autenticar(X509Certificate2 certificado)
        {
            try
            {
                // *** PASO 1: OBTENER SEMILLA ***
                _logger?.LogInformation("[1] Obteniendo semilla...");
                var semillaResponse = await _http.GetAsync(URL_SEMILLA);
                
                if (!semillaResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Error obteniendo semilla: {semillaResponse.StatusCode}");
                }
                
                // Leer como bytes para evitar problemas de encoding
                var semillaBytes = await semillaResponse.Content.ReadAsByteArrayAsync();
                string semillaXml = Encoding.UTF8.GetString(semillaBytes);
                
                _logger?.LogInformation("Semilla recibida (primeros 200 chars): {SemillaPreview}", 
                    semillaXml.Substring(0, Math.Min(200, semillaXml.Length)));

                // *** LIMPIEZA: Eliminar BOM y declaración XML ***
                semillaXml = LimpiarXML(semillaXml);
                
                _logger?.LogInformation("Semilla limpia (primeros 200 chars): {SemillaLimpiaPreview}", 
                    semillaXml.Substring(0, Math.Min(200, semillaXml.Length)));

                // *** PASO 2: FIRMAR SEMILLA ***
                _logger?.LogInformation("[2] Firmando semilla...");
                var semillaFirmada = _signer.FirmarECF(semillaXml, certificado);
                
                // Guardar para debug
                await File.WriteAllTextAsync("semilla_firmada_debug.xml", semillaFirmada);
                _logger?.LogInformation("Semilla firmada guardada en: semilla_firmada_debug.xml");

                // *** PASO 3: VALIDAR ESTRUCTURA ANTES DE ENVIAR ***
                ValidarXMLFirmado(semillaFirmada);

                // *** PASO 4: ENVIAR A VALIDAR (Obtener Token) ***
                _logger?.LogInformation("[3] Enviando semilla firmada para validación...");
                
                // CRÍTICO: Content-Type debe ser "application/xml" sin charset
                var content = new StringContent(semillaFirmada, Encoding.UTF8, "application/xml");
                
                // Forzar que NO se agregue charset automáticamente
                content.Headers.ContentType!.CharSet = null;
                
                var tokenResponse = await _http.PostAsync(URL_TOKEN, content);
                
                // Leer respuesta
                var responseBytes = await tokenResponse.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(responseBytes);
                
                _logger?.LogInformation("Respuesta código: {StatusCode}", tokenResponse.StatusCode);
                _logger?.LogInformation("Respuesta body: {ResponseBody}", responseString);

                if (!tokenResponse.IsSuccessStatusCode) 
                {
                    throw new Exception($"Error DGII ({tokenResponse.StatusCode}): {responseString}");
                }

                // *** PASO 5: EXTRAER TOKEN ***
                string tokenLimpio = ExtraerToken(responseString);
                _logger?.LogInformation("Token obtenido (primeros 50 chars): {TokenPreview}...", 
                    tokenLimpio.Substring(0, Math.Min(50, tokenLimpio.Length)));
                
                return tokenLimpio;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR en Autenticar: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<string> EnviarFactura(string xmlFirmado, string token)
        {
            try
            {
                _logger?.LogInformation("[ENVÍO] Enviando factura...");
                
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                _http.DefaultRequestHeaders.Add("Accept", "application/xml");

                var content = new StringContent(xmlFirmado, Encoding.UTF8, "application/xml");
                content.Headers.ContentType!.CharSet = null;
                
                var response = await _http.PostAsync(URL_RECEPCION, content);
                
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(responseBytes);
                
                _logger?.LogInformation("Respuesta envío: {StatusCode}", response.StatusCode);
                _logger?.LogInformation("Body: {ResponseBody}", responseString);

                return responseString;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR en EnviarFactura: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        // *** MÉTODOS AUXILIARES ***
        
        private string LimpiarXML(string xml)
        {
            // 1. Eliminar BOM (Byte Order Mark) invisible
            if (xml.StartsWith("\uFEFF")) 
                xml = xml.Substring(1);
            
            // 2. Eliminar \r\n al inicio si existe
            xml = xml.TrimStart('\r', '\n', ' ', '\t');
            
            // 3. Buscar directamente el inicio de <SemillaModel
            int indexSemilla = xml.IndexOf("<SemillaModel");
            if (indexSemilla > 0)
            {
                // Hay contenido antes de <SemillaModel> (probablemente <?xml...?>)
                xml = xml.Substring(indexSemilla);
            }
            else if (indexSemilla < 0)
            {
                // No encontró <SemillaModel>, intentar limpiar declaración XML genéricamente
                if (xml.StartsWith("<?xml"))
                {
                    int endDeclaration = xml.IndexOf("?>");
                    if (endDeclaration > 0)
                    {
                        xml = xml.Substring(endDeclaration + 2);
                    }
                }
            }
            
            // 4. Limpiar espacios/saltos de línea al inicio y final
            xml = xml.Trim();
            
            return xml;
        }

        private void ValidarXMLFirmado(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                
                // Verificar que existe el nodo Signature
                var ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                
                var signatureNode = doc.SelectSingleNode("//ds:Signature", ns);
                if (signatureNode == null)
                {
                    throw new Exception("XML firmado no contiene nodo <Signature>");
                }
                
                // Verificar CanonicalizationMethod
                var canonNode = doc.SelectSingleNode("//ds:CanonicalizationMethod", ns);
                if (canonNode != null)
                {
                    var algorithm = canonNode.Attributes?["Algorithm"]?.Value;
                    _logger?.LogInformation("Canonicalización detectada: {Algorithm}", algorithm);
                    
                    // Debe ser C14N estándar
                    if (algorithm != "http://www.w3.org/TR/2001/REC-xml-c14n-20010315")
                    {
                        _logger?.LogWarning("⚠️ ADVERTENCIA: Algoritmo de canonicalización inesperado: {Algorithm}", algorithm);
                    }
                }
                
                // Verificar SignatureMethod
                var sigMethodNode = doc.SelectSingleNode("//ds:SignatureMethod", ns);
                if (sigMethodNode != null)
                {
                    var algorithm = sigMethodNode.Attributes?["Algorithm"]?.Value;
                    _logger?.LogInformation("Método de firma detectado: {Algorithm}", algorithm);
                }
                
                _logger?.LogInformation("✓ Validación de estructura XML firmado: OK");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error validando XML firmado: {ex.Message}");
            }
        }

        private string ExtraerToken(string xmlResponse)
        {
            try
            {
                // El responseString es XML de DGII formato:
                // <AutenticacionModel><token>...</token>...</AutenticacionModel>
                
                var doc = new XmlDocument();
                doc.LoadXml(xmlResponse);
                
                // Buscar nodo token (puede o no tener namespace)
                var tokenNode = doc.SelectSingleNode("//token");
                if (tokenNode != null)
                {
                    return tokenNode.InnerText;
                }
                
                // Fallback: buscar manualmente
                if (xmlResponse.Contains("<token>"))
                {
                    int start = xmlResponse.IndexOf("<token>") + 7;
                    int end = xmlResponse.IndexOf("</token>");
                    if (end > start)
                    {
                        return xmlResponse.Substring(start, end - start);
                    }
                }
                
                throw new Exception("No se pudo extraer el token del XML de respuesta");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR extrayendo token. XML recibido: {XmlResponse}", xmlResponse);
                throw;
            }
        }
    }
}