using System;
using Newtonsoft.Json;

namespace GGroupp.Internal.Timesheet;

internal sealed record class BotMenuCommandJson
{
    [JsonProperty("commandId")]
    public Guid? Id { get; init; }

    [JsonProperty("commandName")]
    public string? Name { get; init; }
}