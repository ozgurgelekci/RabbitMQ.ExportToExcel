using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.ExportToExcel.FileCreateWorkerService;
using RabbitMQ.ExportToExcel.FileCreateWorkerService.Models;
using RabbitMQ.ExportToExcel.FileCreateWorkerService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        ConfigurationManager configurationManager = new ConfigurationManager();

        services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(configurationManager.GetConnectionString("RabbitMQ")), DispatchConsumersAsync = true });

        services.AddSingleton<RabbitMQClientService>();

        services.AddHostedService<Worker>();

        services.AddDbContext<AdventureWorks2019Context>(options =>
        {
            options.UseSqlServer(configurationManager.GetConnectionString("SqlServer"));
        });
    })
    .Build();

await host.RunAsync();
