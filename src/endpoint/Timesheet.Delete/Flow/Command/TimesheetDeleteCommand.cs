using System.Text.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class TimesheetDeleteCommand(ICrmTimesheetApi crmTimesheetApi) : ITimesheetDeleteCommand
{
    private static readonly JsonSerializerOptions SerializerOptions
        =
        new(JsonSerializerDefaults.Web);
}