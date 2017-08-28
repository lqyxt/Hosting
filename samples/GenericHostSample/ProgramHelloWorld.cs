using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class ProgramHelloWorld
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<IHostedService, MyServiceA>();
                    services.AddScoped<IHostedService, MyServiceB>();
                })
                .Build();

            host.StartAsync().GetAwaiter().GetResult();

            Console.WriteLine("Started!");
            Console.ReadKey();

            host.StopAsync().GetAwaiter().GetResult();

            Console.WriteLine("Stopped!");
        }
    }
}
