using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SummaCore.Services
{
    public class DgiiService
    {
        private readonly HttpClient _http;
        private readonly SignerService _signer;
        private readonly ILogger<DgiiService>? _logger;
        // =========================================================================
    // RUTAS OFICIALES - AMBIENTE CERTIFICACIÓN (CerteCF) - SEGÚN PDF
    // =========================================================================
    
    // 1. Autenticación (Semilla)
    // Base PDF: https://eCF.dgii.gov.do/CerteCF/Autenticacion
    private const string URL_SEMILLA = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
    
    // 2. Validación Semilla (Token)
    private const string URL_TOKEN = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla";
    
    // 3. Recepción de Facturas (CORREGIDO)
    // Base PDF: https://eCF.dgii.gov.do/CerteCF/Recepcion
    // Endpoint: /api/FacturasElectronicas
    private const string URL_RECEPCION = "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas";

    // 4. Consulta de Estado (TrackId) - Para cuando recibas el TrackId
    // Base PDF: https://eCF.dgii.gov.do/CerteCF/ConsultaResultado
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
            _http.DefaultRequestHeaders.Add("Accept", "application/xml");
        }

       public async Task<string> Autenticar(X509Certificate2 certificado)
        {
            try
            {
                // [1] Obtener Semilla (Igual que antes)
                _logger?.LogInformation("[1] Obteniendo semilla...");
                var semillaResponse = await _http.GetAsync(URL_SEMILLA);
                semillaResponse.EnsureSuccessStatusCode();
                
                string semillaXml = Encoding.UTF8.GetString(await semillaResponse.Content.ReadAsByteArrayAsync());
                semillaXml = LimpiarXML(semillaXml); // Tu método LimpiarXML existente está bien

                // [2] Firmar Semilla (Usando el nuevo SignerService que limpia el XML)
                _logger?.LogInformation("[2] Firmando semilla...");
                var semillaFirmada = _signer.FirmarECF(semillaXml, certificado);

                // [3] Validar Semilla (CAMBIO IMPORTANTE: Usar MultipartFormData)
                _logger?.LogInformation("[3] Enviando semilla firmada para validación...");

                using var content = new MultipartFormDataContent();
                // TypeScript usa el nombre "xml" para el campo y un nombre de archivo dummy como "signed.xml"
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(semillaFirmada));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                content.Add(fileContent, "xml", "semilla_firmada.xml");

                var tokenResponse = await _http.PostAsync(URL_TOKEN, content);
                
                var responseString = await tokenResponse.Content.ReadAsStringAsync();
                
                if (!tokenResponse.IsSuccessStatusCode) 
                {
                    throw new Exception($"Error DGII ({tokenResponse.StatusCode}): {responseString}");
                }

                // [4] Extraer Token (Tu método existente funciona)
                string tokenLimpio = ExtraerToken(responseString);
                return tokenLimpio;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR en Autenticar");
                throw;
            }
        }

        // Agrega el parámetro 'nombreArchivo' para cumplir con el estándar (RNC+e-NCF.xml)
        public async Task<string> EnviarFactura(string xmlFirmado, string token, string nombreArchivo = "factura.xml")
        {
            try
            {
                _logger?.LogInformation("[ENVÍO] Enviando factura...");
                
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                // TypeScript no fuerza Accept header aquí, pero dejarlo no debería dañar.
                
                // CAMBIO IMPORTANTE: Usar MultipartFormData
                using var content = new MultipartFormDataContent();
                
                // Convertir string a bytes para evitar problemas de encoding al crear el contenido
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(xmlFirmado));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                
                // TypeScript: formData.append('xml', stream, options);
                // El nombre del campo DEBE ser "xml"
                content.Add(fileContent, "xml", nombreArchivo);

                var response = await _http.PostAsync(URL_RECEPCION, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                _logger?.LogInformation("Respuesta envío: {StatusCode}", response.StatusCode);
                _logger?.LogInformation("Body: {ResponseBody}", responseString);
                if (!response.IsSuccessStatusCode)
                  {
                     // Esto hará que el error salga en el JSON de respuesta de tu controlador
                    throw new Exception($"Error DGII al enviar factura ({response.StatusCode}): {responseString}");
                }

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

        private string ExtraerToken(string xml)
        {
            if (xml.Contains("<token>"))
            {
                int start = xml.IndexOf("<token>") + 7;
                int end = xml.IndexOf("</token>");
                if (end > start) return xml.Substring(start, end - start);
            }
            return xml;
        }
        public async Task<string> ConsultarEstado(string trackId, string token)
        {
            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                // La URL base ya es: .../api/Consultas/Estado
                // Concatenamos el parámetro trackId
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
                    // Retornamos un JSON de error manual para no romper el bucle del controlador
                    return $"{{\"trackId\": \"{trackId}\", \"estado\": \"ERROR_INTERNO\", \"mensaje\": \"{ex.Message}\"}}";
                }
        }
    }

}