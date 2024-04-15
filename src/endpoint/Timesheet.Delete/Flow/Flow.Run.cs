using GarageGroup.Infra.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class DeleteTimesheetFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context,
        string commandName,
        ICrmTimesheetApi timesheetApi,
        CancellationToken cancellationToken)
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

        var timesheet = GetWebAppDeleteResponseJson(context);

        await chatFlow.RunFlow(context, timesheetApi, timesheet).GetFlowStateAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlowStarter<TimesheetDeleteFlowState>?> GetChatFlowAsync(
        this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var starter = context.GetChatFlowStarter<TimesheetDeleteFlowState>("TimesheetDelete");
        if (await starter.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return starter;
        }

        var timesheet = GetWebAppDeleteResponseJson(context);
        if (timesheet?.Timesheet is not null && string.Equals(timesheet.Command, commandName, StringComparison.InvariantCultureIgnoreCase))
        {
            return starter;
        }

        return null;
    }

    private static WebAppDeleteResponseJson? GetWebAppDeleteResponseJson(IBotContext context)
    {
        var dataWebApp = TelegramWebAppResponse.FromChannelData(context.TurnContext.Activity.ChannelData);
        return JsonConvert.DeserializeObject<WebAppDeleteResponseJson>((dataWebApp.Message?.WebAppData?.Data).OrEmpty());
    }
}