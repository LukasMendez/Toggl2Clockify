using Toggl2ClockifyService.Options;
using Toggl2ClockifyServiceTransferingService;
using Toggl2ClockifyServiceTransferingService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration config = hostContext.Configuration;
        services.AddHostedService<Worker>();
        services.Configure<TogglOptions>(config.GetSection("Toggl"));
        services.Configure<ClockifyOptions>(config.GetSection("Clockify"));
        services.Configure<Toggl2ClockifyOptions>(config.GetSection("Toggl2Clockify"));
        services.Configure<GeneralConfigurationOptions>(config.GetSection("GeneralConfiguration"));
        services.AddSingleton<Toggl2ClockifyTransferingService>();
    })
    .Build();

await host.RunAsync();


