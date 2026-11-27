using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentMessageDependency
{
    internal static Dependency<IChatCommand<AgentMessageCommandIn, Unit>> UseAgentMessageCommand<TVoiceFileApi>(
        this Dependency<TVoiceFileApi, AgentVoiceOption, AgentChatFeature> dependency)
        where TVoiceFileApi : IAgentVoiceFileApi
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Fold<IChatCommand<AgentMessageCommandIn, Unit>>(
            static (voiceFileApi, option, chatFeature) => new AgentMessageCommand(voiceFileApi, option, chatFeature));
    }
}
