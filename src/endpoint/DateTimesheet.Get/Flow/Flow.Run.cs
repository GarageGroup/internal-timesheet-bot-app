﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetSetGetChatFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context, string commandName, ICrmTimesheetApi timesheetApi, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timesheetApi);

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

        await chatFlow.RunFlow(context.ConversationState, timesheetApi).CompleteValueAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlow?> GetChatFlowAsync(this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var chatFlow = context.CreateChatFlow("TimesheetSetGet");
        if (await chatFlow.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return chatFlow;
        }

        if (context.TurnContext.RecognizeCommandOrAbsent(commandName).IsPresent)
        {
            return chatFlow;
        }

        return null;
    }
}