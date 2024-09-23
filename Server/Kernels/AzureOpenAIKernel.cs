using Microsoft.SemanticKernel;

namespace BlazorSemanticKernelApp.Server.Kernels
{
    public class AzureOpenAIKernel
    {
        private readonly Kernel _kernel;
        public AzureOpenAIKernel(IConfiguration configuration)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                configuration.GetSection("AzureOpenAIDeployment").Value,
                configuration.GetSection("AzureOpenAIAPIAddress").Value,
                configuration.GetSection("AzureOpenAIApiKey").Value
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
