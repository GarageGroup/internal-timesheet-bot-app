using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class HourValueAwaitFlowStep
{

    internal static ChatFlow<TimesheetCreateFlowState> AwaitHourValue(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitValue(
            HourValueAwaitHelper.GetStepOption,
            HourValueAwaitHelper.ParseHourValueOrFailure,
            HourValueAwaitHelper.GetResultMessage,
            (state, value) => state with
            {
                ValueHours = value
            });
}