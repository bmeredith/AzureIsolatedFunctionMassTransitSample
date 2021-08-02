using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace MassTransitClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            var services = new ServiceCollection();

            services.TryAddSingleton<ILoggerFactory>(new SerilogLoggerFactory());
            services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((_, cfg) =>
                {
                    cfg.Host(config["rabbitmqHostname"], config["rabbitmqVirtualHost"], hostConfig =>
                    {
                        hostConfig.Username(config["rabbitmqUsername"]);
                        hostConfig.Password(config["rabbitmqPassword"]);
                    });
                });
            });

            await using var provider = services.BuildServiceProvider(true);

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var startTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

            logger.LogInformation("Started");
            await Task.Run(() => Client(provider), CancellationToken.None);
        }

        private static async Task Client(IServiceProvider provider)
        {
            var logger = provider.GetRequiredService<ILogger<Program>>();

            while (true)
            {
                Console.Write("Enter # of patrons to visit, or empty to quit: ");
                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    break;

                int limit;
                int loops = 1;
                var segments = line.Split(',');
                if (segments.Length == 2)
                {
                    loops = int.TryParse(segments[1], out int result) ? result : 1;
                    limit = int.TryParse(segments[0], out result) ? result : 1;
                }
                else if (!int.TryParse(line, out limit))
                    limit = 1;

                logger.LogInformation("Running {LoopCount} loops of {Limit} patrons each", loops, limit);

                using var serviceScope = provider.CreateScope();

                var publisher = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var random = new Random();

                for (var pass = 0; pass < loops; pass++)
                {
                    try
                    {
                        var tasks = new List<Task>();
                        var patronIds = NewId.Next(limit);

                        for (var i = 0; i < limit; i++)
                        {
                            var enteredTask = publisher.Publish<PatronEntered>(new
                            {
                                PatronId = patronIds[i],
                                Timestamp = DateTime.UtcNow
                            });

                            var leftTask = publisher.Publish<PatronLeft>(new
                            {
                                PatronId = patronIds[i],
                                Timestamp = DateTime.UtcNow + TimeSpan.FromMinutes(random.Next(60))
                            });

                            tasks.Add(enteredTask);
                            tasks.Add(leftTask);
                        }

                        await Task.WhenAll(tasks.ToArray());
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Loop Faulted");
                    }
                }
            }
        }
    }
}
