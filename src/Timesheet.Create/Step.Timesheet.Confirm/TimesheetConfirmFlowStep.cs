using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetConfirmFlowStep
{
    private const string CanceledText = "Списание времени было отменено";

    internal static ChatFlow<TimesheetCreateFlowStateJson> ConfirmCreation(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.SendActivity(
            TimesheetConfirmActivity.CreateActivity)
        .ForwardValue(
            async (context, token) =>
            {
                var result = context.GetConfifrmationResult();

                if (result is ConfirmationResultCode.Canceled)
                {
                    var message = MessageFactory.Text(CanceledText);
                    _ = await context.SendActivityAsync(message, token).ConfigureAwait(false);

                    return context.CancelAction();
                }

                return result is ConfirmationResultCode.Confirmed ? context.NextSameAction() : context.AwaitAndRetrySameAction();
            });
}