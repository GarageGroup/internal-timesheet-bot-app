using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateChatFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context,
        string commandName,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi crmTimesheetApi,
        TimesheetCreateFlowOption option,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(crmProjectApi);
        ArgumentNullException.ThrowIfNull(crmTimesheetApi);
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

        var data = GetWebAppUpdateResponseJson(context);

        await chatFlow.RunFlow(context, crmProjectApi, crmTimesheetApi, option, data).GetFlowStateAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlowStarter<TimesheetCreateFlowState>?> GetChatFlowAsync(
        this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var starter = context.GetChatFlowStarter<TimesheetCreateFlowState>("TimesheetCreate");
        if (await starter.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return starter;
        }

        if (context.TurnContext.RecognizeCommandOrAbsent(commandName).IsPresent)
        {
            return starter;
        }

        var timesheet = GetWebAppUpdateResponseJson(context);
        if (string.Equals(timesheet?.Command, "updatetimesheet", StringComparison.InvariantCultureIgnoreCase))
        {
            await context.TurnContext.DeleteActivityAsync(context.TurnContext.Activity.Id, cancellationToken).ConfigureAwait(false);
            return starter;
        }

        return null;
    }

    private static WebAppDataTimesheetUpdateJson? GetWebAppUpdateResponseJson(IBotContext context)
    {
        var dataWebApp = TelegramWebAppResponse.FromChannelData(context.TurnContext.Activity.ChannelData);
        return JsonConvert.DeserializeObject<WebAppDataTimesheetUpdateJson>((dataWebApp.Message?.WebAppData?.Data).OrEmpty());
    }
}