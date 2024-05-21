using System.Text.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class TimesheetCreateCommand : ITimesheetCreateCommand
{
    private static readonly JsonSerializerOptions SerializerOptions
        =
        new(JsonSerializerDefaults.Web);

    private readonly ICrmTimesheetApi crmTimesheetApi;

    private readonly ICrmProjectApi crmProjectApi;

    private readonly TimesheetCreateFlowOption option;

    internal TimesheetCreateCommand(ICrmTimesheetApi crmTimesheetApi, ICrmProjectApi crmProjectApi, TimesheetCreateFlowOption option)
    {
        this.crmTimesheetApi = crmTimesheetApi;
        this.crmProjectApi = crmProjectApi;
        this.option = option;
    }
}