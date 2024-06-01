using Microsoft.EntityFrameworkCore;
using MonitoringService;
using MonitoringService.Domain.Models;
using MonitoringService.Interfaces;
using MonitoringService.Persistence;
using MonitoringService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<ConfigurableSettings>(context.Configuration.GetSection("Options"));
        services.Configure<EmailProperties.EmailSettings>(context.Configuration.GetSection("EmailSettings"));
        services.AddSingleton<EmailService>();
        services.AddSingleton<FileDirectorySetup>();
        services.AddSingleton<CsvFileManagement>();
        services.AddSingleton<NewFileManagment>();
        services.AddSingleton<ParsePdfs>();
        services.AddSingleton<SpecDetailsManagement>();
        services.AddSingleton<SpecDbOperations>();
        services.AddSingleton<ILogging, Logging>(); 
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();