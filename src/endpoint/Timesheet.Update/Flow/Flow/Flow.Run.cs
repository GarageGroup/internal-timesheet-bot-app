using GarageGroup.Infra.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class UpdateTimesheetFlow
{
    internal static async ValueTask<Unit> RunAsync(
        this IBotContext context, 
        string commandName,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi timesheetApi, 
        TimesheetUpdateOption option, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(crmProjectApi);
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

        var timesheet = GetWebAppUpdateResponseJson(context);

        await chatFlow.RunFlow(context ,crmProjectApi, timesheetApi, timesheet, option).CompleteValueAsync(cancellationToken).ConfigureAwait(false);
        return await context.BotFlow.EndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ChatFlow?> GetChatFlowAsync(this IBotContext context, string commandName, CancellationToken cancellationToken)
    {
        var chatFlow = context.CreateChatFlow("TimesheetUpdate");
        if (await chatFlow.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return chatFlow;
        }

        var timesheet = GetWebAppUpdateResponseJson(context);

        if (timesheet is not null && timesheet.Command?.Equals(commandName) is true)
        {
            return chatFlow;
        }

        return null;
    }

    private static UpdateTimesheetJson? GetWebAppUpdateResponseJson(IBotContext context)
    {
        var dataWebApp = TelegramWebAppResponse.FromChannelData(context.TurnContext.Activity.ChannelData);
        return JsonConvert.DeserializeObject<UpdateTimesheetJson>((dataWebApp.Message?.WebAppData?.Data).OrEmpty());
    }
}