using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toggl2ClockifyService.Options;

public class ClockifyOptions
{
    public string? ApiKey { get; set; }

    public string? WorkspaceId { get; set; }

    public string? UserId { get; set; }
}

