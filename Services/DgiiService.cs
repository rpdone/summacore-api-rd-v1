using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

namespace SummaCore.Services
{
    public class DgiiService
    {
        private readonly HttpClient _http;
        private readonly SignerService _signer;
        
        // URLs del ambiente de CERTIFICACIÓN (Pruebas)
        private const string URL_SEMILLA = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
        private const string URL_TOKEN = "https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla";
        private const string URL_RECEPCION = "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/eCF";

        public DgiiService(SignerService signer)
        {
            _http = new HttpClient();
            _signer = signer;
        }

        public async Task<string> Autenticar(X509Certificate2 certificado)
        {
            // 1. Obtener Semilla
            var semillaXml = await _http.GetStringAsync(URL_SEMILLA);

            // 2. Firmar Semilla (Usamos el mismo firmador)
            var semillaFirmada = _signer.FirmarECF(semillaXml, certificado);

            // 3. Pedir Token
            var content = new StringContent(semillaFirmada, Encoding.UTF8, "application/xml");
            var response = await _http.PostAsync(URL_TOKEN, content);
            
            if (!response.IsSuccessStatusCode) 
                throw new Exception("Error obteniendo token: " + await response.Content.ReadAsStringAsync());

            // El response es un XML/JSON con el token. Simplificamos asumiendo JSON para el ejemplo:
            var responseString = await response.Content.ReadAsStringAsync();
            // Aquí deberías parsear el JSON para sacar el token real. 
            // Por ahora retornamos el string crudo para depuración.
            return responseString; 
        }

        public async Task<string> EnviarFactura(string xmlFirmado, string token)
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var content = new StringContent(xmlFirmado, Encoding.UTF8, "application/xml");
            var response = await _http.PostAsync(URL_RECEPCION, content);

            return await response.Content.ReadAsStringAsync();
        }
    }
}