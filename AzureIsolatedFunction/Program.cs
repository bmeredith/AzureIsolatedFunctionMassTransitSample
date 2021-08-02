using System.Threading.Tasks;
using AzureIsolatedFunction.Components;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace AzureIsolatedFunction
{
    public class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<WarmupFunction>();

                    var logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                        .CreateLogger();
                    services.AddLogging(builder => builder.AddSerilog(logger));
                    
                    var hostname = hostContext.Configuration["rabbitmqHostname"];
                    var virtualHost = hostContext.Configuration["rabbitmqVirtualHost"];
                    var username = hostContext.Configuration["rabbitmqUsername"];
                    var password = hostContext.Configuration["rabbitmqPassword"];
                    services.AddMassTransit(x =>
                    {
                        x.AddSagaStateMachine<PatronStateMachine, PatronState>()
                            .InMemoryRepository();
                        x.AddConsumer<PatronVisitedConsumer>();

                        x.UsingRabbitMq((c, r) =>
                        {
                            r.Host(hostname, virtualHost, hostConfig =>
                            {
                                hostConfig.Username(username);
                                hostConfig.Password(password);
                            });

                            r.ConfigureEndpoints(c);
                        });
                    });
                    services.AddMassTransitHostedService(true);
                })
                .Build();

            await host.RunAsync();
        }
    }
}