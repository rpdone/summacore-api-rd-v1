using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using SummaCore.Models;
using System.Text.Json; 
using System.Text;

namespace SummaCore.Services
{
    public class DgiiService
    {
        private readonly HttpClient _http;
        private readonly SignerService _signer;
        private readonly ILogger<DgiiService>? _logger;
        private const string URL_SEMILLA = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
        private const string URL_TOKEN = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla";
        private const string URL_RECEPCION = "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas";
        private const string URL_CONSULTA = "https://ecf.dgii.gov.do/CerteCF/ConsultaResultado/api/Consultas/Estado";

        public DgiiService(SignerService signer, ILogger<DgiiService>? logger = null)
        {
            var handler = new HttpClientHandler { UseDefaultCredentials = false, PreAuthenticate = false };
            _http = new HttpClient(handler);
            _signer = signer;
            _logger = logger;
            _http.DefaultRequestHeaders.Add("Accept", "application/json"); 
        }

        public async Task<(string Response, string Token)> Facturar(ECF ecf)
        {
            try
            {
                if (ecf.Encabezado.IdDoc.FechaVencimientoSecuencia == "#e") 
                    ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = null;

                string xmlContent = SerializarObjeto(ecf);

                _logger?.LogInformation("XML Generado para e-NCF {eNCF}:\n{XML}", 
                    ecf.Encabezado.IdDoc.eNCF, xmlContent);

                string token = await Autenticar();
                string xmlFirmado = _signer.SignXML(xmlContent);

                _logger?.LogDebug("XML Firmado:\n{XMLFirmado}", xmlFirmado);

                string nombreArchivo = $"{ecf.Encabezado.Emisor.RNCEmisor}{ecf.Encabezado.IdDoc.eNCF}.xml";
                string respuesta = await EnviarFactura(xmlFirmado, token, nombreArchivo);

                _logger?.LogInformation("Respuesta DGII para {eNCF}: {Respuesta}", 
                    ecf.Encabezado.IdDoc.eNCF, respuesta);

                return (respuesta, token);
            }
            catch (Exception ex) 
            { 
                _logger?.LogError(ex, "Error en Facturación"); 
                throw; 
            }
        }

        public async Task<string> Autenticar()
        {
            var semillaResponse = await _http.GetAsync(URL_SEMILLA);
            semillaResponse.EnsureSuccessStatusCode();
            string semillaXml = Encoding.UTF8.GetString(await semillaResponse.Content.ReadAsByteArrayAsync());
            semillaXml = LimpiarXML(semillaXml); 
            var semillaFirmada = _signer.SignXML(semillaXml);
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(semillaFirmada));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            content.Add(fileContent, "xml", "semilla_firmada.xml");
            var tokenResponse = await _http.PostAsync(URL_TOKEN, content);
            var responseString = await tokenResponse.Content.ReadAsStringAsync();
            if (!tokenResponse.IsSuccessStatusCode) throw new Exception($"Error DGII: {responseString}");
            return ExtraerToken(responseString);
        }

        public async Task<string> EnviarFactura(string xmlFirmado, string token, string nombreArchivo)
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Trim('"')}");
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(xmlFirmado));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            content.Add(fileContent, "xml", nombreArchivo);
            var response = await _http.PostAsync(URL_RECEPCION, content);
            return await response.Content.ReadAsStringAsync();
        }

        private string LimpiarXML(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return "";
            string clean = xml.StartsWith("\uFEFF") ? xml.Substring(1) : xml;
            int index = clean.IndexOf("<SemillaModel");
            return index > -1 ? clean.Substring(index) : clean;
        }

        private string SerializarObjeto(ECF ecf)
        {
            var serializer = new XmlSerializer(typeof(ECF));
            var namespaces = new XmlSerializerNamespaces(); namespaces.Add("", "");
            using (var stringWriter = new StringWriterUtf8()) { serializer.Serialize(stringWriter, ecf, namespaces); return stringWriter.ToString(); }
        }

        private (bool EsValido, string Error) ValidarXmlContraXsd(string xmlContent, string xsdPath)
        {
            try {
                var schemas = new XmlSchemaSet(); schemas.Add("", XmlReader.Create(new StreamReader(xsdPath)));
                var document = new XmlDocument(); document.LoadXml(xmlContent); document.Schemas.Add(schemas);
                string errores = "";
                document.Validate((o, e) => { errores += $"- {e.Message}\n"; });
                return (string.IsNullOrEmpty(errores), errores);
            } catch (Exception ex) { return (false, ex.Message); }
        }

        public class StringWriterUtf8 : StringWriter { public override Encoding Encoding => Encoding.UTF8; }

        private string ExtraerToken(string response)
        {
            try { using (JsonDocument doc = JsonDocument.Parse(response)) { if (doc.RootElement.TryGetProperty("token", out JsonElement t)) return t.GetString(); } } catch { }
            if (response.Contains("<token>")) { int s = response.IndexOf("<token>") + 7; int e = response.IndexOf("</token>"); if (e > s) return response.Substring(s, e - s); }
            return response;
        }

        public async Task<string> ConsultarEstado(string trackId, string token)
        {
             _http.DefaultRequestHeaders.Clear(); _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
             var response = await _http.GetAsync($"{URL_CONSULTA}?trackId={trackId}");
             return await response.Content.ReadAsStringAsync();
        }
    }
}