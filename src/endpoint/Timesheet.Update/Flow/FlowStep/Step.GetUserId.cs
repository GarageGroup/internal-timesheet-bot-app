using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetUpdateFlowStep
{
    internal static ChatFlow<UpdateTimesheetFlowState> GetUserId(this ChatFlow<UpdateTimesheetFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}