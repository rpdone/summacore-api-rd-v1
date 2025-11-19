using Microsoft.AspNetCore.Mvc;

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
}