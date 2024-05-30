using Microsoft.EntityFrameworkCore;
using MonitoringService;
using MonitoringService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<ConfigurableSettings>(context.Configuration.GetSection("Options"));
        services.Configure<EmailSettings>(context.Configuration.GetSection("EmailSettings"));
        services.AddSingleton<EmailService>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();

