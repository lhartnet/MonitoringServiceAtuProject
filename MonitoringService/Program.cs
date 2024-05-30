using Microsoft.EntityFrameworkCore;
using MonitoringService;
using Microsoft.Extensions.Configuration;
using MonitoringService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<ConfigurableSettings>(context.Configuration.GetSection("FolderPaths"));
        services.Configure<EmailSettings>(context.Configuration.GetSection("EmailSettings"));
        services.AddSingleton<EmailService>();
        services.AddHostedService<Worker>();
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
    })
    .Build();

host.Run();
