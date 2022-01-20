using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toggl2ClockifyService.Options;

public class Toggl2ClockifyOptions
{
    /// <summary>
    /// The projects that need to be mapped from Toggl to Clockify
    /// </summary>
    public ProjectMapping[]? ProjectMappings { get; set; } 
}

public class ProjectMapping
{
    /// <summary>
    /// The project sources that should be mapped to Clockify identified by their Id
    /// </summary>
    public int[]? SourceProjectIds { get; set; }

    /// <summary>
    /// The destination project id in Clockify
    /// </summary>
    public string? DestinationProjectId { get; set; }
}
