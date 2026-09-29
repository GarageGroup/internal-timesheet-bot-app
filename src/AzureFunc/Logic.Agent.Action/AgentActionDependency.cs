using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentActionDependency
{
    internal static Dependency<IChatCommand<AgentActionCommandIn, Unit>> UseAgentActionCommand<TAgentApi>(
        this Dependency<TAgentApi> dependency)
        where TAgentApi : IAgentActionApi
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<IChatCommand<AgentActionCommandIn, Unit>>(static api => new AgentActionCommand(api));
    }
}
