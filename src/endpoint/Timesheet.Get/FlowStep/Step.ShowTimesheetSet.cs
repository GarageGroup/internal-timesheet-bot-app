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
        chatFlow.ReplaceActivityOrSkip(
            CreateActivity);

    private static IActivity CreateActivity(IChatFlowContext<TimesheetGetFlowState> context)
    {
        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return context.CreateTextActivity($"Нет списаний времени за {context.FlowState.Date?.ToStringRussianCulture()}");
        }

        if (context.IsCardSupported())
        {
            return CreateAdaptiveCardActivity(context);
        }

        if (context.IsTelegramChannel())
        {
            return context.Pipe(BuildTelegramText).Pipe(context.CreateTelegramTextActivity);
        }

        var text = BuildText(context);
        return MessageFactory.Text(text);
    }

    private static IActivity CreateTextActivity(this IChatFlowContext<TimesheetGetFlowState> turnContext, string text)
        =>
        turnContext.IsNotTelegramChannel() ? MessageFactory.Text(text) : turnContext.CreateTelegramTextActivity(text);

    private static IActivity CreateAdaptiveCardActivity(IChatFlowContext<TimesheetGetFlowState> context)
        =>
        new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.GetAdaptiveSchemaVersion())
            {
                Body = CreateAdaptiveBody(context)
            }
        }
        .ToActivity();

    private static Activity CreateTelegramTextActivity(this IChatFlowContext<TimesheetGetFlowState> context, string text)
    {
        var textBuilder = new StringBuilder(text)
            .Append(TelegramBotLine)
            .Append(LineSeparator)
            .Append(TelegramBotLine)
            .Append("/newtimesheet - Списать время");

        var webAppUrl = context.FlowState.BuildWebAppUrl();
        context.Logger.LogInformation("WebAppUrl: {webAppUrl}", webAppUrl);

        return new(ActivityTypes.Message)
        {
            ChannelData = BuildChannelData(context.FlowState, textBuilder.ToString(), webAppUrl).ToJObject()
        };

        static TelegramChannelData BuildChannelData(TimesheetGetFlowState flowState, string text, string webAppUrl)
        {
            if (flowState.Timesheets?.Count is not > 0)
            {
                return new(
                    parameters: new(text)
                    {
                        ParseMode = TelegramParseMode.Html,
                        ReplyMarkup = new TelegramReplyKeyboardRemove()
                    });
            }

            return new(
                parameters: new(text)
                {
                    ParseMode = TelegramParseMode.Html,
                    ReplyMarkup = new TelegramReplyKeyboardMarkup(
                        [
                            [
                                new("Редактировать")
                                {
                                    WebApp = new(webAppUrl)
                                }
                            ]
                        ])
                    {
                        OneTimeKeyboard = true,
                        ResizeKeyboard = true
                    }
                });
        }
    }

    private static List<AdaptiveElement> CreateAdaptiveBody(IChatFlowContext<TimesheetGetFlowState> context)
    {
        var adaptiveElements = new List<AdaptiveElement>();

        if (string.IsNullOrEmpty(context.FlowState.MessageText) is false)
        {
            adaptiveElements.Add(CreateAdaptiveMessageRow(context.FlowState.MessageText));
        }

        adaptiveElements.Add(
            CreateAdaptiveTimesheetRow(context.FlowState.GetDurationSum(), $"Всего за {context.FlowState.Date?.ToStringRussianCulture()}"));

        if (context.FlowState.Timesheets is null)
        {
            return adaptiveElements;
        }

        foreach (var timesheet in context.FlowState.Timesheets)
        {
            var timesheetRow = CreateAdaptiveTimesheetRow(timesheet.Duration, timesheet.Project?.Name);
            adaptiveElements.Add(timesheetRow);

            if (string.IsNullOrEmpty(timesheet.Description) is false)
            {
                var descriptionRow = CreateAdaptiveDescriptionRow(timesheet.Description);
                adaptiveElements.Add(descriptionRow);
            }
        }

        return adaptiveElements;
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
                            Text = duration.ToDurationStringRussianCulture(),
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

    private static string BuildText(IChatFlowContext<TimesheetGetFlowState> context)
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
            flowState.GetDurationSum().ToDurationStringRussianCulture(true),
            context.EncodeTextWithStyle($"Всего за {context.FlowState.Date?.ToStringRussianCulture()}", BotTextStyle.Bold));

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
                timesheet.Duration.ToDurationStringRussianCulture(true), context.EncodeTextWithStyle(timesheet.Project?.Name, BotTextStyle.Bold));

            if (string.IsNullOrEmpty(timesheet.Description))
            {
                return row;
            }

            return row.Append(BotLine).AppendFormat(
                context.EncodeTextWithStyle(timesheet.Description, BotTextStyle.Italic));
        }
    }

    private static string BuildTelegramText(IChatFlowContext<TimesheetGetFlowState> context)
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
            flowState.GetDurationSum().ToDurationStringRussianCulture(true), $"<b>Всего за {flowState.Date?.ToStringRussianCulture()}</b>");

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
                timesheet.Duration.ToDurationStringRussianCulture(true), $"<b>{HttpUtility.HtmlEncode(timesheet.Project?.Name)}</b>");

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
            dateText: state.Date?.ToStringRussianCulture(),
            timesheets: state.Timesheets,
            allowedDays: state.AllowedIntervalInDays);

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