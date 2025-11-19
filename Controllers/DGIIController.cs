using Microsoft.AspNetCore.Mvc;
using SummaCore.Models;
using SummaCore.Services;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace SummaCore.Controllers
{
    // Ruta base obligatoria "/fe" según documentación técnica
    [Route("fe")]
    [ApiController]
    public class DGIIController : ControllerBase

    {
        // 1. Endpoint de Recepción
        // Ruta final: /fe/recepcion/api/ecf
        [HttpPost("recepcion/api/ecf")]
        public IActionResult RecibirECF([FromBody] object xmlData)
        {
            // Aquí conectarás tu lógica de validación más adelante
            return Ok(new { mensaje = "Recibido en SummaCore .NET 10" });
        }

        // 2. Endpoint de Aprobación Comercial
        // Ruta final: /fe/aprobacioncomercial/api/ecf
        [HttpPost("aprobacioncomercial/api/ecf")]
        public IActionResult RecibirAprobacion([FromBody] object xmlData)
        {
            return Ok(new { mensaje = "Aprobación recibida" });
        }

        // 3. Endpoint de Autenticación
        // Ruta final: /fe/autenticacion/api/semilla
        [HttpGet("autenticacion/api/semilla")]
        public IActionResult GetSemilla()
        {
            return Ok(new { semilla = "0000-0000-0000" });
        }
    }
    
    [Route("api/pruebas")] // Nueva ruta para tus pruebas internas
    [ApiController]
    public class PruebasController : ControllerBase
    {
        private readonly SignerService _signer;
        private readonly DgiiService _dgiiSender;

        public PruebasController()
        {
            _signer = new SignerService();
            _dgiiSender = new DgiiService(_signer);
        }

        [HttpGet("ejecutar-paso2")]
        public async Task<IActionResult> EjecutarPaso2()
        {
            try 
            {
                // 1. Cargar Certificado (Súbelo a tu proyecto en VS Code)
                // OJO: En producción usa Azure Key Vault. Para este tutorial, archivo local.
                var rutaCert = Path.Combine(Directory.GetCurrentDirectory(), "certificado.p12");
                var passCert = "TU_CLAVE_DEL_CERTIFICADO"; // ¡Cámbialo!
                var certificado = new X509Certificate2(rutaCert, passCert);

                // 2. Autenticarse con DGII
                var tokenResponse = await _dgiiSender.Autenticar(certificado);
                // NOTA: Aquí debes extraer el token limpio del XML/JSON que retorna DGII.
                // Para efectos de prueba, asume que tienes el token string.
                string token = "TOKEN_EXTRAIDO"; // Implementar parser simple

                // 3. Generar XML basado en el CSV (Fila 4: Crédito Fiscal)
                // RNC Emisor: Tu RNC (del nombre del archivo Excel 131487272)
                // RNC Comprador: 101796361 (del Excel)
                // Monto: 1500.00
                // Tipo: 31
                
                var factura = new ECF
                {
                    Encabezado = new Encabezado
                    {
                        IdDoc = new IdDoc 
                        { 
                            TipoeCF = 31, 
                            eNCF = "E310000000001", // Secuencia inicial de prueba
                            FechaVencimientoSecuencia = "31-12-2025"
                        },
                        Emisor = new Emisor 
                        { 
                            RNCEmisor = "131487272", // TU RNC
                            RazonSocialEmisor = "TU EMPRESA",
                            FechaEmision = DateTime.Now.ToString("dd-MM-yyyy")
                        },
                        Comprador = new Comprador
                        {
                            RNCComprador = "101796361", // Del Excel Fila 4
                            RazonSocialComprador = "CLIENTE DE PRUEBA"
                        },
                        Totales = new Totales
                        {
                            MontoTotal = 1500.00m // Del Excel Fila 4
                        }
                    }
                };

                // 4. Serializar a String XML
                var stringWriter = new StringWriter();
                var serializer = new XmlSerializer(typeof(ECF));
                serializer.Serialize(stringWriter, factura);
                string xmlSinFirma = stringWriter.ToString();

                // 5. Firmar
                string xmlFirmado = _signer.FirmarECF(xmlSinFirma, certificado);

                // 6. Enviar
                string resultado = await _dgiiSender.EnviarFactura(xmlFirmado, token);

                return Ok(new { mensaje = "Prueba enviada", respuestaDgii = resultado });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}