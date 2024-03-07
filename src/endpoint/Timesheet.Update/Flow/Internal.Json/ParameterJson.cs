using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record ParameterJson
{
    [JsonProperty("reply_markup")]
    public InlineKeyboardMarkupJson? ReplyMarkup { get; init; }
}
