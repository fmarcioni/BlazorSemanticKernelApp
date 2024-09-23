using Microsoft.AspNetCore.Mvc;

namespace BlazorSemanticKernelApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadFileController : Controller
    {
        private readonly string _targetFilePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "Shared\\Files\\Input");

        public UploadFileController()
        {
            // Crea la directory se non esiste
            if (!Directory.Exists(_targetFilePath))
            {
                Directory.CreateDirectory(_targetFilePath);
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File non valido");

            string extension = file.FileName.Split(".")[1].ToLower();
            string targetDirectory = $"{_targetFilePath}\\{extension}";

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            var filePath = Path.Combine(targetDirectory, file.FileName);
            if (Directory.GetFiles(targetDirectory).Contains(filePath))
                return BadRequest("File già caricato");

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return Ok(new { message = "File caricato con successo!" });
        }

        [HttpGet("get-csv-files")]
        public IActionResult GetCsvFiles()
        {
            var csvDirectory = Path.Combine(_targetFilePath, "csv");

            if (!Directory.Exists(csvDirectory))
                return NotFound("Directory non trovata.");

            var files = Directory.GetFiles(csvDirectory, "*.csv").Select(Path.GetFileName).ToList();

            return Ok(files);
        }
    }
}
