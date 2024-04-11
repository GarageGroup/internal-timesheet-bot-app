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

        var timesheets = GetWebAppDeleteResponseJson(context);
        if (timesheets is null)
        {
            return await context.BotFlow.NextAsync(cancellationToken).ConfigureAwait(false);
        }

        await chatFlow.RunFlow(context, timesheetApi, timesheets).CompleteValueAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlow?> GetChatFlowAsync(this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var chatFlow = context.CreateChatFlow("TimesheetDelete");
        if (await chatFlow.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return chatFlow;
        }

        var timesheets = GetWebAppDeleteResponseJson(context);

        if (timesheets is not null && timesheets.Command?.Equals(commandName) is true)
        {
            return chatFlow;
        }

        return null;
    }

    private static WebAppDeleteResponseJson? GetWebAppDeleteResponseJson(IBotContext context)
    {
        var dataWebApp = TelegramWebAppResponse.FromChannelData(context.TurnContext.Activity.ChannelData);
        return JsonConvert.DeserializeObject<WebAppDeleteResponseJson>((dataWebApp.Message?.WebAppData?.Data).OrEmpty());
    }
}