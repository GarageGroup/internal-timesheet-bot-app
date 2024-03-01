using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record ReplyMarkupJson
{
    [JsonProperty("keyboard")]
    public KeyboardButtonJson[][]? KeyboardButtons { get; init; }

    [JsonProperty("resize_keyboard")]
    public bool ResizeKeyboar { get; init; }

    [JsonProperty("one_time_keyboard")]
    public bool OneTimeKeyboard { get; init; }

    [JsonProperty("input_field_placeholder")]
    public string? InputFieldPlaceholder { get; init; }
}
