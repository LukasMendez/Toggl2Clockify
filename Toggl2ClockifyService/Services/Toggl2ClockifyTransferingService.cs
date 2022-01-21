using Clockify.Net;
using Clockify.Net.Models.TimeEntries;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Reflection;
using Toggl.QueryObjects;
using Toggl.Services;
using Toggl2ClockifyService.Models;
using Toggl2ClockifyService.Options;
using Task = System.Threading.Tasks.Task;

namespace Toggl2ClockifyServiceTransferingService.Services;

public class Toggl2ClockifyTransferingService
{
    private readonly TogglOptions togglOptions;
    private readonly ClockifyOptions clockifyOptions;
    private readonly Toggl2ClockifyOptions toggl2ClockifyOptions;
    private readonly GeneralConfigurationOptions generalConfigurationOptions;
    private readonly ILogger<Toggl2ClockifyTransferingService> logger;

    public Toggl2ClockifyTransferingService(
        IOptions<TogglOptions> togglOptions,
        IOptions<ClockifyOptions> clockifyOptions,
        IOptions<Toggl2ClockifyOptions> toggl2ClockifyOptions,
        IOptions<GeneralConfigurationOptions> generalConfigurationOptions,
        ILogger<Toggl2ClockifyTransferingService> logger)
    {
        this.togglOptions = togglOptions.Value;
        this.clockifyOptions = clockifyOptions.Value;
        this.toggl2ClockifyOptions = toggl2ClockifyOptions.Value;
        this.generalConfigurationOptions = generalConfigurationOptions.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Will peform all the necessary steps to accumulate the tracked hours for a certain period and export them to Clockify
    /// </summary>
    public async Task TransferTimeReports()
    {

        try
        {
            DateTime startDate = GetStartDate();
            var endDate = DateTime.Now;

            ProjectMapping[]? projectMappings = this.toggl2ClockifyOptions.ProjectMappings;

            if (projectMappings == null)
            {
                this.logger.LogInformation("No project mappings were found in the App Settings file.");
                return;
            }

            foreach (ProjectMapping? projectMapping in projectMappings)
            {
                // Handle all all the project mappings one by one
                // E.g. Toggle_Project_A, contains: 1a, 2a, 3a and will be mapped to Clockify_Project_A
                // In next iteration it will be Toggle_Project_B etc.
                if (projectMapping?.SourceProjectIds != null)
                {
                    // IMPORT: Get and process the time reports from Toggl

                    // Returns a list of projects with all of their time reports from Toggl
                    List<IReadOnlyCollection<TimeReportEntry>> togglReportsPerProject = projectMapping.SourceProjectIds.Select(projectId => GetTogglTimeReport(startDate, endDate, projectId)).ToList();

                    // Concatenate all the lists 
                    List<TimeReportEntry> summarizedTimeReportForEntireProject = ConcatTimeReports(togglReportsPerProject);

                    this.logger.LogInformation($"From {startDate} to {endDate} you have worked {summarizedTimeReportForEntireProject.Sum(entry => entry.TotalHours)} hours.");

                    // EXPORT: Export the time reports to Clockify
                    if (projectMapping.DestinationProjectId == null)
                    {
                        throw new NullReferenceException("No destination project Id has been specified");
                    }
                    await ExportToClockify(summarizedTimeReportForEntireProject, projectMapping.DestinationProjectId, startDate, endDate);

                }

            }

            // Will save the current date for next time
            SaveExecutionDate(endDate);

            this.logger.LogInformation($"The last execution date is: {startDate}");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message);
        }

    }

    /// <summary>
    /// Will return the startdate of the timeperiod that the service should read. The start date will 
    /// usually be the last execution date. If none exist, it will be a default value provided by the configuration settings
    /// </summary>
    /// <returns></returns>
    private DateTime GetStartDate()
    {
        string json = File.ReadAllText(Path.Combine(Path.Combine(Assembly.GetExecutingAssembly().Location), "history.json"));
        var history = JsonConvert.DeserializeAnonymousType(json, new
        {
            LastExecution = default(DateTime?)
        });

        return history.LastExecution != null ? (DateTime)history.LastExecution : DateTime.Now.AddDays(-this.generalConfigurationOptions.DefaultDaysSinceNow);
    }


    /// <summary>
    /// Will save the last execution date, which would also usually be the enddate of the time period, 
    /// that was used to extract the report from Toggl
    /// </summary>
    /// <param name="endDate"></param>
    private void SaveExecutionDate(DateTime endDate)
    {
        var json = new
        {
            LastExecution = endDate
        };


        File.WriteAllText(Path.Combine(Path.Combine(Assembly.GetExecutingAssembly().Location),"history.json"), JsonConvert.SerializeObject(json));
    }

    /// <summary>
    /// Will extract a time report for the selected time range and return a list of <see cref="TimeReportEntry"/>
    /// Will also round the amount of hours worked earch day to the nearest 30 minutes if this is configured
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="projectId">The source project Id</param>
    /// <returns></returns>
    private IReadOnlyCollection<TimeReportEntry> GetTogglTimeReport(DateTime startDate, DateTime endDate, long projectId)
    {
        // Toggle client + search criterias 
        string? apiKey = this.togglOptions.ApiKey;
        var timeService = new TimeEntryService(apiKey);
        var timeEntryparams = new TimeEntryParams
        {
            StartDate = startDate.Date,
            EndDate = endDate.AddDays(1), // +1 day is needed to capture the end of the billing range day
            ProjectId = projectId,
        };

        // List to be returned 
        List<TimeReportEntry> timeReports = new();

        // All hours from the time period (startDate to endDate) 
        var hours = timeService.List(timeEntryparams);

        try
        {
            // Group by date and add all durations together
            var hoursByDates = hours
                .Where(hour => hour.Duration != null)
                .GroupBy(hour => DateTime.ParseExact(hour.Start, "MM/dd/yyyy HH:mm:ss", null).Date)
                .Select(group => new TimeReportEntry(group.Key, TimeSpan.FromSeconds(group.Sum(timeEntry => timeEntry.Duration ?? 0))))
                .ToList();

            return hoursByDates;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message);
            return new List<TimeReportEntry>();
        }

    }

    /// <summary>
    /// Used to concatenate the time reports for multiple projects, which can be useful if you have serveral projects that needs to be mapped to one project in Clockify. 
    /// </summary>
    /// <param name="projectTimeReports">A collection of the projects containing all the <see cref="TimeReportEntry"/></param>
    /// <returns></returns>
    private List<TimeReportEntry> ConcatTimeReports(List<IReadOnlyCollection<TimeReportEntry>> projectTimeReports)
    {
        // Select all TimeReport Entries group them by Date and take the sum of all hours for that specific date on all projects and return it
        return projectTimeReports.SelectMany(projectTimeReports => projectTimeReports)
            .GroupBy(timeEntry => timeEntry.Date)
            .Select(group => new TimeReportEntry(group.Key, TimeSpan.FromSeconds(
                group.Sum(timeEntry => timeEntry.WorkTime.TotalSeconds))))
            .ToList();
    }


    /// <summary>
    /// Exports all the time report entries from Toggl to Clockify using a destination project Id. 
    /// The method requires a start and end time in order to check if there are any existing time entries, that need to be deleted before creating the new ones. 
    /// </summary>
    /// <param name="timeReportEntries"></param>
    /// <param name="projectId"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private async Task ExportToClockify(IReadOnlyCollection<TimeReportEntry> timeReportEntries, string projectId, DateTime startTime, DateTime endTime)
    {
        string apiKey = this.clockifyOptions.ApiKey ?? throw new NullReferenceException("ApiKey is null! You need one to export to Clockify");
        string workSpaceId = this.clockifyOptions.WorkspaceId ?? throw new NullReferenceException("WorkspaceId is null! You need one to export to Clockify");
        string userId = this.clockifyOptions.UserId ?? throw new NullReferenceException("UserId is null! You need one to export to Clockify");
        bool shouldRoundTime = this.generalConfigurationOptions.ShouldRoundTime;

        var timeService = new ClockifyClient(apiKey);

        // Get existing time entries for the period, and delete them
        IRestResponse<List<TimeEntryDtoImpl>>? result = await timeService.FindAllTimeEntriesForUserAsync(workSpaceId, userId, null, startTime, endTime, projectId);
        IEnumerable<TimeEntryDtoImpl>? timeEntriesForPeriod = result.Data;
        if (result.IsSuccessful && timeEntriesForPeriod?.Any() == true)
        {
            foreach (TimeEntryDtoImpl? existingTimeEntry in timeEntriesForPeriod)
            {
                if (existingTimeEntry != null)
                {
                    // Delete each time entry as we are going to replace it with new data
                    IRestResponse? response = await timeService.DeleteTimeEntryAsync(workSpaceId, existingTimeEntry.Id);
                    if (!response.IsSuccessful)
                    {
                        this.logger.LogError(response.ErrorMessage);
                    }
                }
            }
        }

        // Add the new entries (Upsert)
        foreach (TimeReportEntry? timeEntry in timeReportEntries)
        {
            TimeEntryRequest timeEntryRequest = new()
            {
                ProjectId = projectId,
                Start = timeEntry.Date, // will just start from 00:00
                End = shouldRoundTime == true ? timeEntry.Date.AddHours(timeEntry.TotalHours) : timeEntry.Date.Add(timeEntry.WorkTime) // Add the amount of worked hours to the start time (00:00 + x) 
            };

            IRestResponse<TimeEntryDtoImpl>? response = await timeService.CreateTimeEntryAsync(workSpaceId, timeEntryRequest);
            if (!response.IsSuccessful)
            {
                this.logger.LogError(response.ErrorMessage);
            }
        }

    }

}

