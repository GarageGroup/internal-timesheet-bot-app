namespace Flow.FlowStep
{
    internal sealed record DeleteTimesheetOptions
    {
        public TimeSpan TimesheetInterval { get; init; }
        public required string UrlWebApp { get; init; }
    }
}
