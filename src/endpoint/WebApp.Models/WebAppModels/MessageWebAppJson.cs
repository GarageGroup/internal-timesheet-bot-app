using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record MessageWebAppJson
{
    [JsonProperty("web_app_data")]
    public WebAppDataJson? WebAppData { get; init; }
}
