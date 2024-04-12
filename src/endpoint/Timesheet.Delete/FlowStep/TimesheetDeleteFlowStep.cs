namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetDeleteFlowStep
{
    private static readonly PipelineParallelOption ParallelOption
        =
        new()
        {
            DegreeOfParallelism = 4
        };
}