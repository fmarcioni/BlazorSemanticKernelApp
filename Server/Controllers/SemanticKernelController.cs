using BlazorSemanticKernelApp.Server.Kernels;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Newtonsoft.Json;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;


namespace BlazorSemanticKernelApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemanticKernelController : ControllerBase
    {
        
        private readonly IConfiguration _configuration;

        public SemanticKernelController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("riconciliazione")]
        public async Task<IActionResult> Riconciliazione([FromBody] RiconciliazioneRequest request)
        {
            Kernel kernel = new AzureOpenAIKernel(_configuration).Kernel;

            // Prendi il primo file CSV nella cartella
            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var file = Path.Combine($"{basePath}\\Shared\\Files\\Riconciliazione\\Input\\csv\\", request.CsvFile);

            // Carica il template dal file
            string systemPromptPath = System.IO.File.ReadAllText($"{basePath}\\Shared\\Files\\Prompts\\System\\{request.SystemPrompt}");

            // Imposta i parametri per l'esecuzione del prompt
            AzureOpenAIPromptExecutionSettings settings = new AzureOpenAIPromptExecutionSettings
            {
                Temperature = 0.1,
                MaxTokens = 4096
            };

            // Inizializza una lista per contenere le righe del CSV
            List<string> csvLines = new List<string>();
            using (var reader = new StreamReader(file))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                    csvLines.Add(line);
            }

            // Suddividi le righe in gruppi di 20
            int batchSize = 20;
            var batches = csvLines.Select((line, index) => new { line, index })
                                  .GroupBy(x => x.index / batchSize)
                                  .Select(group => group.Select(x => x.line).ToList())
                                  .ToList();

            // Lista di task per le chiamate parallele
            List<Task<string>> tasks = new List<Task<string>>();

            foreach (var batch in batches)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Creare un prompt per ogni batch
                    string batchContent = string.Join("\n", batch);
                    string systemPrompt = systemPromptPath.Replace("{{fileContent}}", batchContent);
                    systemPrompt = $"{systemPrompt}\n {request.UserPrompt}";

                    var function = kernel.CreateFunctionFromPrompt(systemPrompt, settings);

                    // Gestione del timeout per evitare blocchi infiniti
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromMinutes(2));

                    try
                    {
                        // Streaming della risposta per il batch
                        var resultStream = kernel.InvokeStreamingAsync(function, null, cts.Token);
                        StringBuilder fullResult = new StringBuilder();

                        await foreach (var chunk in resultStream)
                            fullResult.Append(chunk);

                        // Restituisci il risultato del batch
                        return fullResult.ToString();
                    }
                    catch (TaskCanceledException)
                    {
                        return "Timeout per il batch";
                    }
                    catch (Exception ex)
                    {
                        return $"Errore per il batch: {ex.Message}";
                    }
                }));
            }

            // Attendi che tutte le chiamate siano completate
            var results = await Task.WhenAll(tasks);

            // Pulizia e deserializzazione dei JSON batch
            var jsonList = new List<MyData>();

            foreach (var batchResult in results)
            {
                // Rimuovi il delimitatore markdown (```json e ```)
                string cleanBatch = batchResult.Replace("```json", "").Replace("```", "").Trim();

                // Deserializza il batch in una lista di oggetti MyData
                var batchData = JsonConvert.DeserializeObject<List<MyData>>(cleanBatch);

                // Aggiungi il contenuto del batch alla lista totale
                if (batchData != null)
                    jsonList.AddRange(batchData);
            }

            // Serializza l'intera lista in un JSON unico
            string finalJson = JsonConvert.SerializeObject(jsonList, Formatting.Indented);

            // Percorso del file CSV di output
            var csvOutputPath = Path.Combine($"{basePath}\\Shared\\Files\\Riconciliazione\\Responses\\csv\\RiconciliazioneResult.csv");

            // Crea e salva il CSV
            using (var writer = new StreamWriter(csvOutputPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(jsonList);
            }

            // Restituisci il risultato finale
            return Ok(finalJson);
        }

        [HttpGet("get-csv-files")]
        public IActionResult GetCsvFiles()
        {
            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var csvDirectory = $"{basePath}\\Shared\\Files\\Riconciliazione\\Input\\csv\\";

            if (!Directory.Exists(csvDirectory))
                return NotFound("Directory non trovata.");

            var files = Directory.GetFiles(csvDirectory, "*.csv").Select(Path.GetFileName).ToList();

            return Ok(files);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File non valido");

            string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            string targetDirectory = $"{basePath}\\Shared\\Files\\Riconciliazione\\Input\\csv\\";

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

    public class RiconciliazioneRequest
    {
        public string SystemPrompt { get; set; }
        public string UserPrompt { get; set; }
        public string CsvFile { get; set; }
    }

    public class MyData
    {
        public string document_number { get; set; }
        public string document_date { get; set; }
        public string owner { get; set; }
        public int tpmovimento { get; set; }
    }
}