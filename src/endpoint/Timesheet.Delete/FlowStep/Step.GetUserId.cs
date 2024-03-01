using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<DeleteTimesheetFlowState> GetUserId(this ChatFlow<DeleteTimesheetFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}