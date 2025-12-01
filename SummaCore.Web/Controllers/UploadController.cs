using Microsoft.AspNetCore.Mvc;
using SummaCore.Processor;
using SummaCore.Services;

namespace SummaCore.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ECFProcessor _processor;
        private readonly IWebHostEnvironment _env;

        public UploadController(ECFProcessor processor, IWebHostEnvironment env)
        {
            _processor = processor;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Process the file
            // Note: In a real app, this should be a background job. For now, we await it.
            // Also, we need an output directory for XMLs.
            var outputDir = Path.Combine(uploadsFolder, "output");
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            try
            {
                await _processor.ProcessExcelFile(filePath, outputDir);
                return Ok(new { Message = "File processed successfully.", OutputDir = outputDir });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
