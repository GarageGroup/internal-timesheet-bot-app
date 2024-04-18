using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetGetFlowStep
{
    internal static ChatFlow<TimesheetGetFlowState> GetUserId(this ChatFlow<TimesheetGetFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}