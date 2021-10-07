#nullable enable

using System;
using System.Text.Json.Serialization;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed record CardDateValueJson
    {
        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; init; }
    }
}