using AdaptiveCards;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetGetFlowStep
{
    internal static ChatFlow<TimesheetGetFlowState> ShowTimesheetSet(this ChatFlow<TimesheetGetFlowState> chatFlow)
        =>
        chatFlow.ReplaceActivityOrSkip(CreateActivity);

    private static IActivity CreateActivity(IChatFlowContext<TimesheetGetFlowState> context)
    {
        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return context.CreateTextActivity($"No timesheets for {context.FlowState.Date?.ToDisplayText()}");
        }

        if (context.IsCardSupported())
        {
            return CreateAdaptiveCard(context).ToActivity();
        }

        if (context.IsTelegramChannel())
        {
            return context.CreateTelegramParameters(context.BuildTelegramText()).BuildActivity();
        }

        return MessageFactory.Text(context.BuildText());
    }

    private static Activity CreateTextActivity(this IChatFlowContext<TimesheetGetFlowState> turnContext, string text)
        =>
        turnContext.IsNotTelegramChannel() ? MessageFactory.Text(text) : turnContext.CreateTelegramParameters(text).BuildActivity();

    private static Attachment CreateAdaptiveCard(IChatFlowContext<TimesheetGetFlowState> context)
        =>
        new()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.GetAdaptiveSchemaVersion())
            {
                Body = CreateAdaptiveElements(context).ToList()
            }
        };

    private static TelegramParameters CreateTelegramParameters(this IChatFlowContext<TimesheetGetFlowState> context, string text)
    {
        var textBuilder = new StringBuilder(text)
            .Append(TelegramBotLine)
            .Append(LineSeparator)
            .Append(TelegramBotLine)
            .Append("/newtimesheet - Create timesheet");

        var webAppUrl = context.FlowState.BuildWebAppUrl();
        context.Logger.LogInformation("WebAppUrl: {webAppUrl}", webAppUrl);

        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return new(textBuilder.ToString())
            {
                ParseMode = TelegramParseMode.Html,
                ReplyMarkup = new TelegramReplyKeyboardRemove()
            };
        }

        return new(textBuilder.ToString())
        {
            ParseMode = TelegramParseMode.Html,
            ReplyMarkup = new TelegramReplyKeyboardMarkup(
                [
                    [
                        new("Edit")
                        {
                            WebApp = new(webAppUrl)
                        }
                    ]
                ])
            {
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            }
        };
    }

    private static IEnumerable<AdaptiveElement> CreateAdaptiveElements(IChatFlowContext<TimesheetGetFlowState> context)
    {
        if (string.IsNullOrEmpty(context.FlowState.MessageText) is false)
        {
            yield return CreateAdaptiveMessageRow(context.FlowState.MessageText);
        }

        yield return CreateAdaptiveTimesheetRow(context.FlowState.GetDurationSum(), $"Total for {context.FlowState.Date?.ToDisplayText()}");

        if (context.FlowState.Timesheets is null)
        {
            yield break;
        }

        foreach (var timesheet in context.FlowState.Timesheets)
        {
            yield return CreateAdaptiveTimesheetRow(timesheet.Duration, timesheet.Project?.Name);

            if (string.IsNullOrEmpty(timesheet.Description))
            {
                continue;
            }

            yield return CreateAdaptiveDescriptionRow(timesheet.Description);
        }
    }

    private static AdaptiveColumnSet CreateAdaptiveMessageRow(string message)
        =>
        new()
        {
            Spacing = AdaptiveSpacing.Default,
            Columns =
            [
                new()
                {
                    Items =
                    [
                        new AdaptiveTextBlock
                        {
                            Text = $"**{message}**",
                            Size = AdaptiveTextSize.Default,
                            Wrap = false
                        }
                    ]
                }
            ]
        };

    private static AdaptiveColumnSet CreateAdaptiveTimesheetRow(decimal duration, string? projectName)
        =>
        new()
        {
            Spacing = AdaptiveSpacing.Default,
            Columns =
            [
                new()
                {
                    Width = TimeColumnWidth,
                    Items =
                    [
                        new AdaptiveTextBlock
                        {
                            Text = duration.ToDurationDisplayText(),
                            Size = AdaptiveTextSize.Default
                        }
                    ]
                },
                new()
                {
                    Items =
                    [
                        new AdaptiveTextBlock
                        {
                            Text = $"**{projectName}**",
                            Size = AdaptiveTextSize.Default,
                            Wrap = false
                        }
                    ]
                }
            ]
        };

    private static AdaptiveColumnSet CreateAdaptiveDescriptionRow(string description)
        =>
        new()
        {
            Spacing = AdaptiveSpacing.None,
            Columns =
            [
                new()
                {
                    Width = TimeColumnWidth
                },
                new()
                {
                    Items =
                    [
                        new AdaptiveTextBlock
                        {
                            Text = $"_{description}_",
                            Size = AdaptiveTextSize.Default,
                            Wrap = true
                        }
                    ]
                }
            ]
        };

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);

    private static string BuildText(this IChatFlowContext<TimesheetGetFlowState> context)
    {
        var flowState = context.FlowState;
        var textBuilder = new StringBuilder();

        if (string.IsNullOrEmpty(flowState.MessageText) is false)
        {
            textBuilder = textBuilder.AppendRow(
                string.Empty, context.EncodeTextWithStyle(flowState.MessageText, BotTextStyle.Bold))
            .Append(BotLine)
            .Append(HeaderLineSeparator)
            .Append(BotLine);
        }

        textBuilder = textBuilder.AppendRow(
            flowState.GetDurationSum().ToDurationDisplayText(true),
            context.EncodeTextWithStyle($"Total for {context.FlowState.Date?.ToDisplayText()}", BotTextStyle.Bold));

        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return textBuilder.ToString();
        }

        foreach (var timesheetText in context.FlowState.Timesheets.Select(BuildTimesheetText))
        {
            textBuilder.Append(BotLine).Append(LineSeparator).Append(BotLine).Append(timesheetText);
        }

        return textBuilder.ToString();

        StringBuilder BuildTimesheetText(TimesheetJson timesheet)
        {
            var row = new StringBuilder().AppendRow(
                timesheet.Duration.ToDurationDisplayText(true), context.EncodeTextWithStyle(timesheet.Project?.Name, BotTextStyle.Bold));

            if (string.IsNullOrEmpty(timesheet.Description))
            {
                return row;
            }

            return row.Append(BotLine).AppendFormat(
                context.EncodeTextWithStyle(timesheet.Description, BotTextStyle.Italic));
        }
    }

    private static string BuildTelegramText(this IChatFlowContext<TimesheetGetFlowState> context)
    {
        var flowState = context.FlowState;
        var textBuilder = new StringBuilder();

        if (string.IsNullOrEmpty(flowState.MessageText) is false)
        {
            textBuilder = textBuilder
                .AppendRow(string.Empty, $"<b>{HttpUtility.HtmlEncode(flowState.MessageText)}</b>")
                .Append(TelegramBotLine).Append(HeaderLineSeparator).Append(TelegramBotLine);
        }

        textBuilder = textBuilder.AppendRow(
            flowState.GetDurationSum().ToDurationDisplayText(true), $"<b>Total for {flowState.Date?.ToDisplayText()}</b>");

        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return textBuilder.ToString();
        }

        foreach (var timesheetText in context.FlowState.Timesheets.Select(BuildTimesheetText))
        {
            textBuilder.Append(TelegramBotLine).Append(LineSeparator).Append(TelegramBotLine).Append(timesheetText);
        }

        return textBuilder.ToString();

        static StringBuilder BuildTimesheetText(TimesheetJson timesheet)
        {
            var row = new StringBuilder().AppendRow(
                timesheet.Duration.ToDurationDisplayText(true), $"<b>{HttpUtility.HtmlEncode(timesheet.Project?.Name)}</b>");

            if (string.IsNullOrEmpty(timesheet.Description))
            {
                return row;
            }

            return row.Append(TelegramBotLine).Append(
                $"<i>{HttpUtility.HtmlEncode(timesheet.Description)}</i>");
        }
    }

    private static StringBuilder AppendRow(this StringBuilder stringBuilder, string first, string second)
        =>
        stringBuilder.AppendFormat("{0,-10}{1}", first, second);

    private static decimal GetDurationSum(this TimesheetGetFlowState flowState)
        =>
        flowState.Timesheets?.Count > 0 ? flowState.Timesheets.Sum(static x => x.Duration) : default;

    private static string BuildWebAppUrl(this TimesheetGetFlowState state)
    {
        var webAppData = new WebAppTimesheetsDataJson(
            date: state.DateText,
            dateText: state.Date?.ToDisplayText(),
            timesheets: state.Timesheets,
            allowedDays: state.LimitationDay);

        var data = webAppData.CompressDataJson();
        return $"{state.UrlWebApp}/selectUpdateTimesheet?data={HttpUtility.UrlEncode(data)}";
    }

    private static string CompressDataJson(this WebAppTimesheetsDataJson data)
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