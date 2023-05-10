using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateChatFlow
{
    internal static async ValueTask<Unit> RunAsync<TTimesheetApi>(
        this IBotContext context, string commandName, TTimesheetApi timesheetApi, CancellationToken cancellationToken)
        where TTimesheetApi : IFavoriteProjectSetGetSupplier, IProjectSetSearchSupplier, ITimesheetCreateSupplier
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

        await chatFlow.RunFlow(context, timesheetApi).CompleteValueAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlow?> GetChatFlowAsync(this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var chatFlow = context.CreateChatFlow("TimesheetCreate");
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