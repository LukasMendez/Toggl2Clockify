using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toggl2ClockifyService.Models;

public class TimeReportEntry
{
    public DateTime Date { get; set; }
    public TimeSpan WorkTime { get; set; }
    public double TotalHours => Math.Round(WorkTime.TotalHours);
    public TimeReportEntry(DateTime date, TimeSpan hoursWorked)
    {
        Date = date;
        WorkTime = hoursWorked;
    }
}
