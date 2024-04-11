using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetUpdateFlowStep
{
    internal static ChatFlow<Unit> ShowDateTimesheet(
        this ChatFlow<TimesheetUpdateFlowState> chatFlow, IBotContext botContext)
        =>
        chatFlow.Next(
            botContext.RunDateTimesheetCommandAsync);

    private static async Task<Unit> RunDateTimesheetCommandAsync(
        this IBotContext botContext, IChatFlowContext<TimesheetUpdateFlowState> context, CancellationToken cancellationToken)
    {
        var contextData = new Dictionary<string, string?>
        {
            ["dateText"] = context.FlowState.DateText,
            ["messageText"] = "Списание времени успешно изменено"
        };

        var stateProperty = botContext.ConversationState.CreateProperty<Dictionary<string, string?>>("timesheetData");
        await stateProperty.SetAsync(context, contextData, cancellationToken).ConfigureAwait(false);

        var activity = context.Activity;

        activity.Text = "datetimesheet";
        if (context.IsTelegramChannel())
        {
            activity.Text = "/" + activity.Text;
        }

        try
        {
            _ = await botContext.BotFlow.NextAsync(activity, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            throw;
        }
        finally
        {
            await stateProperty.DeleteAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return default;
    }
}