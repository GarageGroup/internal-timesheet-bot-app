using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record InlineKeyboardButtonJson
{
    [JsonProperty("text")]
    public string? Text { get; init; }

    [JsonProperty("callback_data")]
    public string? CallbackData { get; init; }
}