using Codeblaze.SemanticKernel.Connectors.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace BlazorSemanticKernelApp.Server.Kernels
{
    public class OllamaKernel
    {
        private readonly Kernel _kernel;
        public OllamaKernel(IConfiguration configuration)
        {
            var builder = Kernel.CreateBuilder();

            builder.Services.AddHttpClient("OllamaClient", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.BaseAddress = new Uri(configuration.GetSection("OllamaAPIAddress").Value);
            });

            builder.AddOllamaChatCompletion(
                configuration.GetSection("OllamaLLM").Value, // Ollama model Id
                configuration.GetSection("OllamaAPIAddress").Value // Ollama endpoint
            );

            builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

            _kernel = builder.Build();
        }

        public Kernel Kernel
        {
            get
            {
                return _kernel;
            }
        }
    }
}
