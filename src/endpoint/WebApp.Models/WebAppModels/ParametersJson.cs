using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record ParametersJson
{
    [JsonProperty("reply_markup")]
    public ReplyMarkupJson? ReplyMarkup { get; init; }
}
