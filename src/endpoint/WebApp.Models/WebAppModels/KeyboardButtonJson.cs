using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record KeyboardButtonJson
{
    [JsonProperty("text")]
    public string? Text { get; init; }

    [JsonProperty("web_app", NullValueHandling = NullValueHandling.Ignore)]
    public WebAppJson? WebApp { get; init; }
}
