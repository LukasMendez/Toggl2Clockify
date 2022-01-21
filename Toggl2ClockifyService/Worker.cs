using Microsoft.Extensions.Options;
using Toggl2ClockifyService.Options;
using Toggl2ClockifyServiceTransferingService.Services;

namespace Toggl2ClockifyServiceTransferingService;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly Toggl2ClockifyTransferingService toggl2ClockifyTransferingService;
    private readonly GeneralConfigurationOptions generalConfigurationOptions;

    public Worker(
        ILogger<Worker> logger,
        Toggl2ClockifyTransferingService toggl2ClockifyTransferingService,
        IOptions<GeneralConfigurationOptions> generalConfigurationOptions)
    {
        this.logger = logger;
        this.toggl2ClockifyTransferingService = toggl2ClockifyTransferingService;
        this.generalConfigurationOptions = generalConfigurationOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            this.logger.LogInformation("Executing scheduled Toggl2Clockify Service");
            await this.toggl2ClockifyTransferingService.TransferTimeReports();
            await Task.Delay(
                TimeSpan.FromHours(this.generalConfigurationOptions.RunIntervalInHours),
                stoppingToken);
        }
    }
}
