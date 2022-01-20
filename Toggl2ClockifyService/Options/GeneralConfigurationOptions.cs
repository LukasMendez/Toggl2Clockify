using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toggl2ClockifyService.Options;

public class GeneralConfigurationOptions
{
    public int DefaultDaysSinceNow { get; set; } = 30;
    public bool ShouldRoundTime { get; set; } = true;
    public int RunIntervalInHours { get; set; } = 8;
}

