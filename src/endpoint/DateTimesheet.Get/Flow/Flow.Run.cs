﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetSetGetChatFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context, string commandName, ICrmTimesheetApi timesheetApi, DateTimesheetEditOption option, CancellationToken cancellationToken)
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

    private static async Task<ChatFlowStarter<DateTimesheetFlowState>?> GetChatFlowAsync(
        this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var starter = context.GetChatFlowStarter<DateTimesheetFlowState>("TimesheetSetGet");
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