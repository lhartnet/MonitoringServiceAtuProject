using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using MonitoringService;
using MonitoringService.Domain.Models;
using MonitoringService.Interfaces;
using MonitoringService.Persistence;
using MonitoringService.Services;
using System.Reflection;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<ConfigurableSettings>(context.Configuration.GetSection("Options"));
        services.Configure<EmailProperties.EmailSettings>(context.Configuration.GetSection("EmailSettings"));
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<FileDirectorySetup>();
        services.AddSingleton<CsvFileManagement>();
        services.AddSingleton<NewFileManagment>();
        services.AddSingleton<ParsePdfs>();
        services.AddSingleton<ISpecDetailsManagement, SpecDetailsManagement>();
        services.AddSingleton<SpecDbOperations>();
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddLog4Net();
    })
    .Build();

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

host.Run();