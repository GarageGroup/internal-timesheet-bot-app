using GarageGroup.Infra.Bot.Builder;
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
        .Forward(
            NextOrRestart)
        .SetTypingStatus();

    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            questionText: context.FlowState.TimesheetId is null ? "Списать время?" : "Сохранить изменения?",
            confirmButtonText: context.FlowState.TimesheetId is null ? "Списать" : "Сохранить",
            cancelButtonText: "Отменить",
            cancelText: context.FlowState.TimesheetId is null ? "Списание времени было отменено" : "Изменение времени было отменено",
            fieldValues:
            [
                new((context.FlowState.Project?.Type.ToStringRussianCulture()).OrEmpty(), context.FlowState.Project?.Name),
                new("Дата", context.FlowState.Date?.ToStringRussianCulture()),
                new("Время", context.FlowState.ValueHours?.ToStringRussianCulture() + "ч"),
                new(string.Empty, context.FlowState.Description?.Value)
            ])
        {
            TelegramWebApp = new($"{context.FlowState.UrlWebApp}/updateTimesheetForm?data={context.FlowState.CreateBase64Data()}")
        };

    private static Result<TimesheetCreateFlowState, BotFlowFailure> GetWebAppData(
        IChatFlowContext<TimesheetCreateFlowState> context, string webAppData)
    {
        return DeserializeWebAppData(webAppData).Pipe(InnerValidateDuration).MapSuccess(BindWebAppData);

        static WebAppCreateTimesheetDataJson DeserializeWebAppData(string webAppData)
            =>
            JsonConvert.DeserializeObject<WebAppCreateTimesheetDataJson>(webAppData);

        static Result<WebAppCreateTimesheetDataJson, BotFlowFailure> InnerValidateDuration(WebAppCreateTimesheetDataJson timesheet)
            =>
            timesheet.Duration is null ? timesheet : ValidateHourValueOrFailure(timesheet.Duration.Value).MapSuccess(_ => timesheet);

        TimesheetCreateFlowState BindWebAppData(WebAppCreateTimesheetDataJson timesheet)
            =>
            context.FlowState with
            {
                Project = timesheet.IsEditProject ? null : context.FlowState.Project,
                UpdateProject = timesheet.IsEditProject,
                Description = timesheet.Description is null ? context.FlowState.Description : new(timesheet.Description),
                ValueHours = timesheet.Duration ?? context.FlowState.ValueHours,
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

    private static string CreateBase64Data(this TimesheetCreateFlowState state)
    {
        var timesheet = new WebAppCreateTimesheetDataJson
        {
            Description = state.Description?.Value.OrEmpty(),
            Duration = state.ValueHours,
            ProjectName = state.Project?.Name
        };

        var webAppDataJson = JsonConvert.SerializeObject(timesheet);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(webAppDataJson));
    }
}