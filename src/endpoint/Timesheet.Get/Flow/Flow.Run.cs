using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetGetChatFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context, string commandName, ICrmTimesheetApi timesheetApi, TimesheetGetFlowOption option, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timesheetApi);
        ArgumentNullException.ThrowIfNull(option);

        var turnContext = context.TurnContext;
        if (turnContext.IsNotMessageType())
        {
            return await context.BotFlow.NextAsync(cancellationToken).ConfigureAwait(false);
        }

        var chatFlow = await context.GetChatFlowAsync(commandName, cancellationToken).ConfigureAwait(false);
        if (chatFlow is null)
        {
            return await context.BotFlow.NextAsync(cancellationToken).ConfigureAwait(false);
        }

        await chatFlow.RunFlow(context.ConversationState, timesheetApi, option).GetFlowStateAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlowStarter<TimesheetGetFlowState>?> GetChatFlowAsync(
        this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var starter = context.GetChatFlowStarter<TimesheetGetFlowState>("TimesheetGet");
        if (await starter.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return starter;
        }

        if (context.TurnContext.RecognizeCommandOrAbsent(commandName).IsPresent)
        {
            return starter;
        }

        return null;
    }
}