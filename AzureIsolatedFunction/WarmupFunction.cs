using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureIsolatedFunction
{
    // required so the Azure Function runtime doesn't complain that there are not Functions to startup
    public class WarmupFunction
    {
        private readonly ILogger<WarmupFunction> _logger;

        public WarmupFunction(ILogger<WarmupFunction> logger)
        {
            _logger = logger;
        }

        [Function("Warmup")]
        public void Run([WarmupTrigger] object warmupContext, FunctionContext context)
        {
            _logger.LogInformation("Function App instance is now warm!");
        }
    }
}
