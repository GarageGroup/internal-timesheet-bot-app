using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> GetUserId(this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}