using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record ChannelDataResponseJson
{
    [JsonProperty("message")]
    public MessageResponseJson? Message { get; init; }

    [JsonProperty("callback_query")]
    public CallbackQueryJson? CallbackQuery { get; init; }
}