using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Text;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ConfirmTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(
            CreateTimesheetConfirmationOption, GetWebAppData)
        .SetTypingStatus(
            GetTempActivity)
        .Forward(
            NextOrRestart);

    private static IActivity? GetTempActivity(IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Project is null)
        {
            return MessageFactory.Text("Изменение проекта...");
        }

        return null;
    }

    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            questionText: "Списать время?",
            confirmButtonText: "Списать",
            cancelButtonText: "Отменить",
            cancelText: "Списание времени было отменено",
            fieldValues:
            [
                new((context.FlowState.Project?.Type.ToStringRussianCulture()).OrEmpty(), context.FlowState.Project?.Name),
                new("Дата", context.FlowState.Date?.ToStringRussianCulture()),
                new("Время", context.FlowState.ValueHours?.ToStringRussianCulture() + "ч"),
                new(string.Empty, context.FlowState.Description?.Value)
            ])
        {
            TelegramWebApp = new($"{context.FlowState.UrlWebApp}/updateTimesheetForm?data={CreateBase64Data(context.FlowState)}")
        };

    private static Result<TimesheetCreateFlowState, BotFlowFailure> GetWebAppData(
        IChatFlowContext<TimesheetCreateFlowState> context, 
        string webAppData)
    {
        var updateTimesheet = JsonConvert.DeserializeObject<EditTimesheetJson>(webAppData);

        if (updateTimesheet is null)
        {
            return BotFlowFailure.From("Произошла ошибка при попытке обработать данные с формы");
        }

        if (updateTimesheet.Duration <= 0 || updateTimesheet.Duration > MaxValue)
        {
            return BotFlowFailure.From($"Длительность должна быть в интервале 0-{MaxValue} часа");
        }

        if (updateTimesheet.IsEditProject is true)
        {
            return context.FlowState with
            {
                Project = null,
                Description = updateTimesheet.Description is null ? context.FlowState.Description : new(updateTimesheet.Description),
                ValueHours = updateTimesheet.Duration ?? context.FlowState.ValueHours,
            };
        }

        return context.FlowState with
        {
            Description = updateTimesheet.Description is null ? context.FlowState.Description : new(updateTimesheet.Description),
            ValueHours = updateTimesheet.Duration ?? context.FlowState.ValueHours,
        };
    }

    private static ChatFlowJump<TimesheetCreateFlowState> NextOrRestart(IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Project is null)
        {
            return ChatFlowJump.Restart(context.FlowState);
        }
        
        return context.FlowState;
    }

    private static string CreateBase64Data(TimesheetCreateFlowState state)
    {
        var timesheet = new EditTimesheetJson()
        {
            Description = state.Description?.Value.OrEmpty(),
            Duration = state.ValueHours,
            ProjectName = state.Project?.Name
        };

        var webAppDataJson = JsonConvert.SerializeObject(timesheet);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(webAppDataJson));
    }
}