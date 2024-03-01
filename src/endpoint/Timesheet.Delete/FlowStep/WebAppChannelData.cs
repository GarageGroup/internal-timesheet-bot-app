using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record WebAppChannelData
{
    [JsonProperty("method")]
    public string? Method { get; init; }

    [JsonProperty("parameters")]
    public Parameters? Parameters { get; init; }
}

internal sealed record Parameters
{

    [JsonProperty("reply_markup")]
    public ReplyMarkup? ReplyMarkup { get; init; }
}

internal sealed record ReplyMarkup
{

    [JsonProperty("keyboard")]
    public KeyboardButton[][]? KeyboardButtons { get; init; }

    [JsonProperty("resize_keyboard")]
    public bool ResizeKeyboar { get; init; }

    [JsonProperty("one_time_keyboard")]
    public bool OneTimeKeyboard { get; init; }

    [JsonProperty("input_field_placeholder")]
    public string? InputFieldPlaceholder { get; init; }
}

internal sealed record KeyboardButton
{
    [JsonProperty("text")]
    public string? Text { get; init; }

    [JsonProperty("web_app")]
    public WebApp? WebApp { get; init; }
}

internal sealed record WebApp
{
    [JsonProperty("url")]
    public string? Url { get; init; }
}