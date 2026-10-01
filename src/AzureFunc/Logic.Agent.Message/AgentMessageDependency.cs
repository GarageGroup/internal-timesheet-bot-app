using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentMessageDependency
{
    internal static Dependency<IChatCommand<AgentMessageCommandIn, Unit>> UseAgentMessageCommand<TAgentApi, TVoiceFileApi>(
        this Dependency<TAgentApi, TVoiceFileApi, AgentVoiceOption> dependency)
        where TAgentApi : IAgentMessageApi
        where TVoiceFileApi : IAgentVoiceFileApi
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IChatCommand<AgentMessageCommandIn, Unit>>(
            static (api, voiceFileApi, option) => new AgentMessageCommand(api, voiceFileApi, option));
    }
}
