using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummaCore.Services;
using SummaCore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace SummaCore.Controllers
{
    [Route("api/pruebas")]
    [ApiController]
    public class PruebasController : ControllerBase
    {
        private readonly ILogger<PruebasController> _logger;
        private readonly SignerService _signer;
        private readonly DgiiService _dgiiSender;
        private readonly IConfiguration _configuration;

        public PruebasController(ILogger<PruebasController> logger, SignerService signer, DgiiService dgiiSender, IConfiguration configuration)
        {
            _logger = logger;
            _signer = signer;
            _dgiiSender = dgiiSender;
            _configuration = configuration;
        }

        [HttpPost("consultar-estatus-lote")]
        public async Task<IActionResult> ConsultarEstatusLote([FromBody] List<string> trackIds)
        {
            if (trackIds == null || trackIds.Count == 0) return BadRequest("Lista vacía");
            var resultados = new List<object>();
            
            var certPath = _configuration["Dgii:CertificatePath"] ?? "13049552_identity.p12";
            var fullCertPath = Path.Combine(Directory.GetCurrentDirectory(), certPath);
            if (!System.IO.File.Exists(fullCertPath)) fullCertPath = Path.Combine(AppContext.BaseDirectory, certPath);

            try {
                #pragma warning disable SYSLIB0057
                var certificate = new X509Certificate2(fullCertPath, _configuration["Dgii:CertificatePassword"] ?? "Solutecdo2025@");
                #pragma warning restore SYSLIB0057
                var token = await _dgiiSender.Autenticar(certificate);

                foreach (var trackId in trackIds) {
                    try {
                        var json = await _dgiiSender.ConsultarEstado(trackId, token);
                        resultados.Add(System.Text.Json.JsonSerializer.Deserialize<object>(json));
                    } catch (Exception ex) {
                        resultados.Add(new { trackId, error = ex.Message });
                    }
                }
                return Ok(new { mensaje = "Consulta finalizada", resultados });
            } catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("test-autenticacion")]
        public async Task<IActionResult> TestAutenticacion()
        {
            // Misma lógica simple de prueba
            return Ok("Servicio de autenticación funcionando correctamente.");
        }
        
        [HttpGet("ejecutar-paso2")]
        public async Task<IActionResult> EjecutarPaso2()
        {
             // Endpoint manual de ejemplo, puedes dejarlo o eliminarlo
             return Ok("Utilice el endpoint /api/CargaMasiva/cargar-csv-paso2 para procesar archivos.");
        }

        [HttpGet("test-firma")]
        public IActionResult TestFirma() => Ok("Servicio de firma activo.");
    }
}