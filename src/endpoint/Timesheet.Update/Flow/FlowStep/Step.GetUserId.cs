using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetUpdateFlowStep
{
    internal static ChatFlow<TimesheetUpdateFlowState> GetUserId(this ChatFlow<TimesheetUpdateFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}