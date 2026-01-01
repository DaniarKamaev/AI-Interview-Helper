using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class SystemLog
{
    public int LogId { get; set; }

    public int? InterviewId { get; set; }

    public int? UserId { get; set; }

    public string LogType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime? CreatedAt { get; set; }
}
