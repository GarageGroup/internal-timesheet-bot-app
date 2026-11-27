using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentSignOutCommandIn : IChatCommandIn<Unit>
{
    public static string Type { get; } = "AgentSignOut";
}
