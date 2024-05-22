using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Web;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetShowFlowStep
{
    internal static ChatFlow<TimesheetShowFlowState> ShowTimesheetSet(this ChatFlow<TimesheetShowFlowState> chatFlow)
        =>
        chatFlow.SendMessageOrSkip(
            CreateTimesheetSetMessage);

    private static ChatMessageSendRequest CreateTimesheetSetMessage(IChatFlowContext<TimesheetShowFlowState> context)
    {
        var builder = context.BuildText()
            .AppendBotLine()
            .AppendFormat(context.User.Culture, "/newtimesheet - {0}", context.Localizer[TimesheetShowResource.TimesheetCommandName]);

        return new(builder.ToString())
        {
            ReplyMarkup = CreateReplyMarkup(context)
        };

        static BotReplyMarkupBase CreateReplyMarkup(IChatFlowContext<TimesheetShowFlowState> context)
        {
            var webAppUrl = context.BuildWebAppUrl();

            if (string.IsNullOrEmpty(webAppUrl))
            {
                return new BotReplyKeyboardRemove();
            }

            context.Logger.LogInformation("WebAppUrl: {webAppUrl}", webAppUrl);

            return new BotReplyKeyboardMarkup
            {
                Keyboard =
                [
                    [
                        new(context.Localizer[TimesheetShowResource.EditButtonName])
                        {
                            WebApp = new(webAppUrl)
                        }
                    ]
                ],
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }
    }

    private static StringBuilder BuildText(this IChatFlowContext<TimesheetShowFlowState> context)
    {
        var builder = new StringBuilder();

        var flowState = context.FlowState;
        var dateText = flowState.Date?.ToString(DateFormat, context.User.Culture);

        if (string.IsNullOrEmpty(flowState.MessageText) is false)
        {
            builder = builder.AppendFormat(
                "<b>{0}</b>{1}{2}{3}", HttpUtility.HtmlEncode(flowState.MessageText), BotBreak, HeaderLineSeparator, BotBreak);
        }

        if (flowState.Timesheets.IsEmpty)
        {
            return builder.AppendFormat(context.Localizer[TimesheetShowResource.NoTimesheetsTemplate], dateText);
        }

        var totalTemplate = context.Localizer[TimesheetShowResource.TotalMessageTemplate];
        var totalMessage = string.Format(context.User.Culture, totalTemplate, dateText);

        var totalDuration = flowState.Timesheets.AsEnumerable().Sum(GetDuration);
        builder = builder.AppendRow(context, totalDuration, totalMessage);

        foreach (var timesheet in context.FlowState.Timesheets)
        {
            builder = builder.AppendBotLine().AppendRow(context, timesheet.Duration, timesheet.Project?.Name);

            if (string.IsNullOrEmpty(timesheet.Description))
            {
                continue;
            }

            builder = builder.Append(BotBreak).AppendFormat("<i>{0}</i>", HttpUtility.HtmlEncode(timesheet.Description));
        }

        return builder;

        static decimal GetDuration(TimesheetJson timesheet)
            =>
            timesheet.Duration;
    }

    private static string? BuildWebAppUrl(this IChatFlowContext<TimesheetShowFlowState> context)
    {
        if (context.FlowState.Timesheets.IsEmpty || context.WebApp is null)
        {
            return null;
        }

        var state = context.FlowState;
        var date = state.Date.GetValueOrDefault();

        var webAppData = new WebAppTimesheetsDataJson(
            date: date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dateText: date.ToString(DateFormat, context.User.Culture),
            timesheets: state.Timesheets,
            allowedDays: state.LimitationDayOfMonth,
            language: context.User.Culture.TwoLetterISOLanguageName);

        var data = webAppData.CompressDataJson();
        return context.WebApp.BuildUrl("selectUpdateTimesheet", [new("data", HttpUtility.UrlEncode(data))]);
    }

    private static string CompressDataJson(this WebAppTimesheetsDataJson data)
    {
        var json = JsonSerializer.Serialize(data, SerializerOptions);

        var buffer = Encoding.UTF8.GetBytes(json);
        var memoryStream = new MemoryStream();

        using (var zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            zipStream.Write(buffer, 0, buffer.Length);
        }

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    private static StringBuilder AppendRow(this StringBuilder builder, IChatFlowContextBase context, decimal duration, string? text)
    {
        var durationText = string.Format(context.User.Culture, "{0:#,##0.00}{1}", duration, context.Localizer[TimesheetShowResource.HourSymbol]);
        return builder.AppendFormat(context.User.Culture, "{0,-10}<b>{1}</b>", durationText, HttpUtility.HtmlEncode(text));
    }

    private static StringBuilder AppendBotLine(this StringBuilder builder)
        =>
        builder.Append(BotBreak).Append(LineSeparator).Append(BotBreak);
}