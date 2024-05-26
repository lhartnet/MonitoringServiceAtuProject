using MonitoringService;
using Microsoft.Extensions.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<ConfigurableSettings>(context.Configuration.GetSection("FolderPaths"));
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
