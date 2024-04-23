using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
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
    {
        var webAppUrl = context.FlowState.BuildWebAppUrl();
        context.Logger.LogInformation("WebAppUrl: {webAppUrl}", webAppUrl);

        return new(
            entity: context.InnerCreateTimesheetCardOption(
                headerText: context.FlowState.TimesheetId is null ? "Списать время?" : "Сохранить изменения?",
                skipStep: context.FlowState.WithoutConfirmation),
            buttons: new(
                confirmButtonText: context.FlowState.TimesheetId is null ? "Списать" : "Сохранить",
                cancelButtonText: "Отменить",
                cancelText: context.FlowState.TimesheetId is null ? "Списание времени было отменено" : "Изменение времени было отменено")
            {
                TelegramWebApp = new(webAppUrl)
            });
    }

    private static EntityCardOption CreateTimesheetCardOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        context.InnerCreateTimesheetCardOption("Изменение списания времени", context.FlowState.WithoutConfirmation  is false);

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
            Description = new(timesheet.Description),
            ValueHours = timesheet.Duration,
            DateText = timesheet.Date,
            WithoutConfirmation = true
        };
    }

    private static ChatFlowJump<TimesheetCreateFlowState> NextOrRestart(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        context.FlowState.Project switch
        {
            null => ChatFlowJump.Restart(context.FlowState with
            {
                WithoutConfirmation = false
            }),
            _ => ChatFlowJump.Next(context.FlowState)
        };

    private static string BuildWebAppUrl(this TimesheetCreateFlowState state)
    {
        var timesheet = new WebAppCreateTimesheetDataJson
        {
            Description = state.Description?.Value.OrEmpty(),
            Duration = state.ValueHours.GetValueOrDefault(),
            Project = state.Project
        };

        var data = timesheet.CompressDataJson();
        return $"{state.UrlWebApp}/updateTimesheetForm?data={HttpUtility.UrlEncode(data)}&date={state.DateText}&days={state.LimitationDay}";
    }

    private static string CompressDataJson(this WebAppCreateTimesheetDataJson data)
    {
        var json = JsonConvert.SerializeObject(data);

        var buffer = Encoding.UTF8.GetBytes(json);
        var memoryStream = new MemoryStream();

        using (var zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            zipStream.Write(buffer, 0, buffer.Length);
        }

        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
