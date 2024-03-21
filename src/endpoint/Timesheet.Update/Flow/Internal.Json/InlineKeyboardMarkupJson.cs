using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record InlineKeyboardMarkupJson
{
    [JsonProperty("inline_keyboard")]
    public InlineKeyboardButtonJson[][]? InlineKeyboard { get; init; }
}