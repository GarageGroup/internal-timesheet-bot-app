using System.Collections.Generic;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetConfirmFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowStateJson> ConfirmCreation(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.AwaitConfirmation(
            CreateConfirmationOption);

    private static ConfirmationCardOption CreateConfirmationOption(TimesheetCreateFlowStateJson flowState)
        =>
        new(
            questionText: "Списать время?",
            confirmButtonText: "Списать",
            cancelButtonText: "Отменить",
            cancelText: "Списание времени было отменен",
            fieldValues: new KeyValuePair<string, string?>[]
            {
                new(flowState.ProjectType.GetName(), flowState.ProjectName),
                new("Дата", flowState.Date.ToStringRussianCulture()),
                new("Время", flowState.ValueHours.ToStringRussianCulture() + "ч"),
                new("Описание", flowState.Description)
            });

    private static string GetName(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Возможная сделка",
            TimesheetProjectType.Lead => "Лид",
            TimesheetProjectType.Incident => "Инцидент",
            _ => "Проект"
        };
}