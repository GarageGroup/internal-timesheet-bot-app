using GarageGroup.Infra.Bot.Builder;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ConfirmTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(CreateTimesheetConfirmationOption, GetWebAppData).SetTypingStatus();

    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            questionText: "Списать время?",
            confirmButtonText: "Списать",
            cancelButtonText: "Отменить",
            cancelText: "Списание времени было отменено",
            fieldValues:
            [
                new(context.FlowState.ProjectType.ToStringRussianCulture(), context.FlowState.ProjectName),
                new("Дата", context.FlowState.Date.ToStringRussianCulture()),
                new("Время", context.FlowState.ValueHours.ToStringRussianCulture() + "ч"),
                new(string.Empty, context.FlowState.Description)
            ])
        {
            TelegramWebApp = new($"{context.FlowState.UrlWebApp}/updateTimesheetForm?data={CreateBase64Data(context.FlowState)}")
        };

    private static Result<TimesheetCreateFlowState, BotFlowFailure> GetWebAppData(
        IChatFlowContext<TimesheetCreateFlowState> context, 
        string webAppData)
    {
        var updateTimesheet = JsonConvert.DeserializeObject<EditTimesheetJson>(webAppData);

        if (updateTimesheet is null) // Такого быть не может, по идеи нужна ошибка.
        {
            return context.FlowState;
        }

        if (updateTimesheet.IsEditProject is true)
        {
            return context.FlowState with
            {
                ProjectId = default,
                ProjectName = default,
                ProjectType = default,
                Description = updateTimesheet.Description ?? context.FlowState.Description,
                ValueHours = updateTimesheet.Duration ?? context.FlowState.ValueHours,
            };
            // Reset flow
        }

        return context.FlowState with
        {
            Description = updateTimesheet.Description ?? context.FlowState.Description,
            ValueHours = updateTimesheet.Duration ?? context.FlowState.ValueHours,
        };
    }

    private static string CreateBase64Data(TimesheetCreateFlowState state)
    {
        var timesheet = new EditTimesheetJson()
        {
            Description = state.Description.OrEmpty(),
            Duration = state.ValueHours,
            ProjectName = state.ProjectName
        };

        var webAppDataJson = JsonConvert.SerializeObject(timesheet);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(webAppDataJson));
    }
}