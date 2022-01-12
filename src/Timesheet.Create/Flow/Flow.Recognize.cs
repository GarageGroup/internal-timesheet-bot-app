using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class TimesheetCreateChatFlow
{
    internal static async ValueTask<Result<ChatFlow, Unit>> InternalRecoginzeOrFailureAsync(
        this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var activity = context.TurnContext.Activity;
        if (activity.IsNotMessageType())
        {
            return default;
        }

        var chatFlow = context.CreateChatFlow("TimesheetCreate");
        if (await chatFlow.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return chatFlow;
        }

        return activity.RecognizeCommandOrAbsnet(commandName).ToResult().MapSuccess(_ => chatFlow);
    }
}