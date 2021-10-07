#nullable enable

using System;
using System.Text.Json.Serialization;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed record UserDataJson
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }
    }
}