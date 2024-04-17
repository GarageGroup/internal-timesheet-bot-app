using GarageGroup.Infra.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Web;

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
            TelegramWebApp = new(context.BuildWebAppUrl())
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
                Project = timesheet.Project,
                Description = timesheet.Description is null ? context.FlowState.Description : new(timesheet.Description),
                ValueHours = timesheet.Duration ?? context.FlowState.ValueHours,
                Date = DateOnly.Parse(timesheet.Date.OrEmpty())
            };
    }

    private static ChatFlowJump<TimesheetCreateFlowState> NextOrRestart(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        context.FlowState.Project switch
        {
            null => ChatFlowJump.Restart(context.FlowState),
            _ => ChatFlowJump.Next(context.FlowState)
        };

    private static string BuildWebAppUrl(this IChatFlowContext<TimesheetCreateFlowState> context)
    {
        var state = context.FlowState;

        var timesheet = new WebAppCreateTimesheetDataJson
        {
            Description = state.Description?.Value.OrEmpty(),
            Duration = state.ValueHours,
            Project = state.Project,
        };

        var webAppDataJson = JsonConvert.SerializeObject(timesheet);

        return $"{state.UrlWebApp}/updateTimesheetForm?data={HttpUtility.UrlEncode(webAppDataJson)}&date={context.FlowState.DateText}&days={context.FlowState.AllowedIntervalInDays}";
    }
}