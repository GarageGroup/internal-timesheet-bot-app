using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentProfileCommandIn : IChatCommandIn<Unit>
{
    internal static readonly AgentProfileCommandIn Instance = new();

    public static string Type { get; } = "AgentProfile";
}
