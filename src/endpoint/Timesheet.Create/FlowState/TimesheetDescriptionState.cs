using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetDescriptionState
{
    public TimesheetDescriptionState(string? value)
        =>
        Value = value;
    

    [JsonProperty("value")]
    public string? Value { get; init; }
}