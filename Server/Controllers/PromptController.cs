using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BlazorSemanticKernelApp.Server.Controllers
{
    [ApiController]
    [Route("api/prompts")]
    public class PromptController : ControllerBase
    {
        [HttpGet("get-files")]
        public IActionResult GetPromptFiles(string folder)
        {
            // Ottieni i file di prompt dalla cartella specificata
            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var folderPath = Path.Combine($"{basePath}\\Shared\\Files\\Prompts\\", folder);
            if (!Directory.Exists(folderPath))
                return NotFound("La cartella specificata non esiste.");

            var files = Directory.GetFiles(folderPath, "*.txt").Select(Path.GetFileName).ToList();
            var json = JsonConvert.SerializeObject(files, Formatting.Indented);

            return Ok(json);
        }

        [HttpGet("get-content")]
        public IActionResult GetPromptContent(string folder, string file)
        {
            // Carica il contenuto del file di prompt
            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var filePath = Path.Combine($"{basePath}\\Shared\\Files\\Prompts\\", folder, file);
            if (!System.IO.File.Exists(filePath))
                return NotFound("Il file richiesto non esiste.");

            var content = System.IO.File.ReadAllText(filePath);
            //var json = JsonConvert.SerializeObject(content, Formatting.Indented);

            return Ok(content);
        }

        [HttpPost("save-content")]
        public IActionResult SavePromptContent(string folder, string file, [FromBody] string content)
        {
            // Salva il contenuto del file di prompt
            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var filePath = Path.Combine($"{basePath}\\Shared\\Files\\Prompts\\", folder, file);
            System.IO.File.WriteAllText(filePath, content.ToString());

            return Ok();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(string type, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File non valido");

            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            string targetDirectory = $"{basePath}\\Shared\\Files\\Prompts\\{type}";

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            var filePath = Path.Combine(targetDirectory, file.FileName);
            if (Directory.GetFiles(targetDirectory).Contains(filePath))
                return BadRequest("File già caricato");

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return Ok(new { message = "File caricato con successo!" });
        }
    }
}