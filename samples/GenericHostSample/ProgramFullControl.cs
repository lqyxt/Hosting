using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class ProgramFullControl
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .UseServiceProviderFactory<MyContainer>(new MyContainerFactory())
                .ConfigureContainer<MyContainer>((hostContext, container) =>
                {
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<IHostedService, MyServiceA>();
                    services.AddScoped<IHostedService, MyServiceB>();
                })
                .Build();

            var s = host.Services;

            host.StartAsync().GetAwaiter().GetResult();

            Console.WriteLine("Started!");

            host.StopAsync().GetAwaiter().GetResult();

            Console.WriteLine("Stopped!");
        }
    }
}
