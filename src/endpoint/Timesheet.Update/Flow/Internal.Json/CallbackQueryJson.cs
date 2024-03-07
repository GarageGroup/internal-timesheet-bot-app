using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record CallbackQueryJson
{
    [JsonProperty("data")]
    public Guid ProjectId { get; init; }
}