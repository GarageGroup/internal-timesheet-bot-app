using Newtonsoft.Json;

namespace Flow.FlowStep
{
    internal sealed record MessageWebApp
    {
        [JsonProperty("web_app_data")]
        public WebAppData? WebAppData { get; init; }
    }

    internal sealed record WebAppResponse
    {
        [JsonProperty("message")]
        public MessageWebApp? Message { get; init; }
    }

    internal sealed record WebAppData
    {
        [JsonProperty("data")]
        public string? Data { get; init; }
    }
}
