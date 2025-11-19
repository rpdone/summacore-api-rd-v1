using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SummaCore.Services
{
    public class DgiiService
    {
        private readonly HttpClient _http;
        private readonly SignerService _signer;
        
        // URLs de AMBIENTE DE CERTIFICACIÓN (Pruebas)
        // Verificadas según documentación técnica
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
            // 1. OBTENER SEMILLA (Bytes crudos para evitar problemas de codificación inicial)
            var semillaBytes = await _http.GetByteArrayAsync(URL_SEMILLA);
            
            // Decodificar UTF-8 manualmente
            string semillaXml = Encoding.UTF8.GetString(semillaBytes);

            // LIMPIEZA 1: Eliminar BOM (Byte Order Mark) invisible si existe al inicio
            if (semillaXml.StartsWith("\uFEFF")) semillaXml = semillaXml.Substring(1);
            
            // LIMPIEZA 2: Eliminar declaración XML si la DGII la envió, para evitar duplicados al firmar
            // Buscamos el inicio del tag raíz <SemillaModel>
            int startIndex = semillaXml.IndexOf("<SemillaModel");
            if (startIndex > 0) semillaXml = semillaXml.Substring(startIndex);

            // 2. FIRMAR SEMILLA
            var semillaFirmada = _signer.FirmarECF(semillaXml, certificado);

            // LIMPIEZA 3 (CRÍTICA): Asegurar que lo que enviamos NO tenga <?xml version...?>
            // Muchos validadores de DGII fallan si el POST body tiene la declaración.
            if (semillaFirmada.StartsWith("<?xml"))
            {
                int rootIndex = semillaFirmada.IndexOf("<SemillaModel");
                if (rootIndex > 0) semillaFirmada = semillaFirmada.Substring(rootIndex);
            }

            // 3. ENVIAR A VALIDAR (Pedir Token)
            // Usamos "application/xml" explícitamente
            var content = new StringContent(semillaFirmada, Encoding.UTF8, "application/xml");
            
            var response = await _http.PostAsync(URL_TOKEN, content);
            
            // Leer respuesta como Bytes primero para evitar los caracteres extraños en el error
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            var responseString = Encoding.UTF8.GetString(responseBytes);

            if (!response.IsSuccessStatusCode) 
            {
                // Lanzar error con el contenido real descifrado
                throw new Exception($"Error DGII ({response.StatusCode}): {responseString}");
            }

            // El responseString aquí es el XML del Token.
            // Ejemplo: <AutenticacionModel><token>...</token>...</AutenticacionModel>
            // Deberías parsearlo para sacar solo el valor del token.
            return responseString; 
        }

        public async Task<string> EnviarFactura(string xmlFirmado, string tokenXml)
        {
            // PARSEAR TOKEN (Extracción rápida del string)
            // Asumimos que tokenXml viene formato XML de DGII.
            // Buscamos el valor dentro de <token>...</token>
            string tokenLimpio = tokenXml;
            if (tokenXml.Contains("<token>"))
            {
                int start = tokenXml.IndexOf("<token>") + 7;
                int end = tokenXml.IndexOf("</token>");
                tokenLimpio = tokenXml.Substring(start, end - start);
            }

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenLimpio);

            var content = new StringContent(xmlFirmado, Encoding.UTF8, "application/xml");
            var response = await _http.PostAsync(URL_RECEPCION, content);

            return await response.Content.ReadAsStringAsync();
        }
    }
}