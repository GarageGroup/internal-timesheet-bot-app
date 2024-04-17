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
        .ShowEntityCard(
            CreateTimesheetCardOption)
        .SetTypingStatus();

    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            entity: context.InnerCreateTimesheetCardOption(
                headerText: context.FlowState.TimesheetId is null ? "Списать время?" : "Сохранить изменения?",
                skipStep: false),
            buttons: new(
                confirmButtonText: context.FlowState.TimesheetId is null ? "Списать" : "Сохранить",
                cancelButtonText: "Отменить",
                cancelText: context.FlowState.TimesheetId is null ? "Списание времени было отменено" : "Изменение времени было отменено")
            {
                TelegramWebApp = new(context.BuildWebAppUrl())
            });

    private static EntityCardOption CreateTimesheetCardOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        context.InnerCreateTimesheetCardOption("Изменение списания времени", context.FlowState.ShowReadonlyCard is false);

    private static EntityCardOption InnerCreateTimesheetCardOption(
        this IChatFlowContext<TimesheetCreateFlowState> context, string headerText, bool skipStep)
        =>
        new(
            headerText: headerText,
            fieldValues:
            [
                new((context.FlowState.Project?.Type.ToStringRussianCulture()).OrEmpty(), context.FlowState.Project?.Name),
                new("Дата", context.FlowState.Date?.ToStringRussianCulture()),
                new("Время", context.FlowState.ValueHours?.ToStringRussianCulture() + "ч"),
                new(string.Empty, context.FlowState.Description?.Value)
            ])
        {
            SkipStep = skipStep
        };

    private static Result<TimesheetCreateFlowState, BotFlowFailure> GetWebAppData(
        IChatFlowContext<TimesheetCreateFlowState> context, string webAppData)
    {
        var timesheet = JsonConvert.DeserializeObject<WebAppCreateTimesheetDataJson>(webAppData);

        return context.FlowState with
        {
            Project = timesheet.Project,
            Description = timesheet.Description is null ? context.FlowState.Description : new(timesheet.Description),
            ValueHours = timesheet.Duration ?? context.FlowState.ValueHours,
            DateText = timesheet.Date,
            ShowReadonlyCard = true
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
            Project = state.Project
        };

        var webAppDataJson = JsonConvert.SerializeObject(timesheet);
        var data = HttpUtility.UrlEncode(webAppDataJson);

        return $"{state.UrlWebApp}/updateTimesheetForm?data={data}&date={context.FlowState.DateText}&days={context.FlowState.AllowedIntervalInDays}";
    }
}
