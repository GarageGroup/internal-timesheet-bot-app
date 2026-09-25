using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentProfileDependency
{
    internal static Dependency<IChatCommand<AgentProfileCommandIn, Unit>> UseAgentProfileCommand<TAgentApi>(
        this Dependency<TAgentApi> dependency)
        where TAgentApi : IAgentProfileApi
        =>
        dependency.Map<IChatCommand<AgentProfileCommandIn, Unit>>(static api => new AgentProfileCommand(api));
}
