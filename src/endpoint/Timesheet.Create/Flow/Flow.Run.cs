using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using GarageGroup.Internal.Timesheet.Internal.Json;
using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateChatFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context,
        string commandName,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi crmTimesheetApi,
        TimesheetEditOption option,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(crmProjectApi);
        ArgumentNullException.ThrowIfNull(crmTimesheetApi);

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

        var timesheetData = GetWebAppUpdateResponseJson(context);

        await chatFlow.RunFlow(context, crmProjectApi, crmTimesheetApi, option, timesheetData).GetFlowStateAsync(cancellationToken).ConfigureAwait(false);
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
        if (timesheet is not null && timesheet.Command?.Equals("updatetimesheet") is true)
        {
            await context.TurnContext.DeleteActivityAsync(context.TurnContext.Activity.Id, cancellationToken).ConfigureAwait(false);
            return starter;
        }

        return null;
    }

    private static WebAppUpdateTimesheetDataJson? GetWebAppUpdateResponseJson(IBotContext context)
    {
        var dataWebApp = TelegramWebAppResponse.FromChannelData(context.TurnContext.Activity.ChannelData);
        return JsonConvert.DeserializeObject<WebAppUpdateTimesheetDataJson>((dataWebApp.Message?.WebAppData?.Data).OrEmpty());
    }
}