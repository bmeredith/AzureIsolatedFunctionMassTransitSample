using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AzureIsolatedFunction.Components
{
    public class PatronVisitedConsumer : IConsumer<PatronVisited>
    {
        readonly ILogger<PatronVisitedConsumer> _logger;

        public PatronVisitedConsumer(ILogger<PatronVisitedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PatronVisited> context)
        {
            _logger.LogInformation("Patron Visited: {PatronId} {Entered} {Left} {Duration}", context.Message.PatronId,
                context.Message.Entered, context.Message.Left, context.Message.Left - context.Message.Entered);

            return Task.CompletedTask;
        }
    }
}
