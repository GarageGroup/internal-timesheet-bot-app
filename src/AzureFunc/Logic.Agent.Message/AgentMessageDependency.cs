using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentMessageDependency
{
    internal static Dependency<IChatCommand<AgentMessageCommandIn, Unit>> UseAgentMessageCommand<TAgentApi>(
        this Dependency<TAgentApi> dependency)
        where TAgentApi : IAgentMessageApi
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<IChatCommand<AgentMessageCommandIn, Unit>>(static api => new AgentMessageCommand(api));
    }
}
