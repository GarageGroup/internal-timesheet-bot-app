﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using AdaptiveCards;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetSetShowHelper
{
    private const string BotLine = "\n\r\n\r";

    private const string TelegramBotLine = "\n\r";

    private const string TimeColumnWidth = "45px";

    private static readonly string LineSeparator = new('-', 50);

    private static readonly string HeaderLineSeparator = new('_', 43);

    internal static IActivity? CreateSuccessActivity(IChatFlowContext<DateTimesheetFlowState> context)
    {
        if (string.IsNullOrEmpty(context.FlowState.MessageText))
        {
            return null;
        }

        if (context.IsNotTelegramChannel())
        {
            return MessageFactory.Text(context.FlowState.MessageText);
        }

        var telegramActivity = context.Activity.CreateReply();

        telegramActivity.ChannelData = new TelegramChannelData(
            parameters: new TelegramParameters(context.FlowState.MessageText)
            {
                DisableNotification = true
            })
            .ToJObject();

        return telegramActivity;
    }

    internal static IActivity CreateActivity(IChatFlowContext<DateTimesheetFlowState> context)
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

    private static IActivity CreateTextActivity(this ITurnContext turnContext, string text)
        =>
        turnContext.IsNotTelegramChannel() ? MessageFactory.Text(text) : turnContext.CreateTelegramTextActivity(text);

    private static IActivity CreateAdaptiveCardActivity(IChatFlowContext<DateTimesheetFlowState> context)
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

    private static IActivity CreateTelegramTextActivity(this ITurnContext context, string text)
    {
        var textBuilder = new StringBuilder(text)
            .Append(TelegramBotLine)
            .Append(LineSeparator)
            .Append(TelegramBotLine)
            .Append("/newtimesheet - Списать время");

        var channelData = new TelegramChannelData(
            parameters: new TelegramParameters(textBuilder.ToString())
            {
                ParseMode = TelegramParseMode.Html,
                ReplyMarkup = new TelegramReplyKeyboardRemove()
            });

        var telegramActivity = context.Activity.CreateReply();
        telegramActivity.ChannelData = channelData.ToJObject();

        return telegramActivity;
    }

    private static List<AdaptiveElement> CreateAdaptiveBody(IChatFlowContext<DateTimesheetFlowState> context)
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
            var timesheetRow = CreateAdaptiveTimesheetRow(timesheet.Duration, timesheet.ProjectName);
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
            Columns = new()
            {
                new()
                {
                    Items = new()
                    {
                        new AdaptiveTextBlock
                        {
                            Text = $"**{message}**",
                            Size = AdaptiveTextSize.Default,
                            Wrap = false
                        }
                    }
                }
            }
        };

    private static AdaptiveColumnSet CreateAdaptiveTimesheetRow(decimal duration, string? projectName)
        =>
        new()
        {
            Spacing = AdaptiveSpacing.Default,
            Columns = new()
            {
                new()
                {
                    Width = TimeColumnWidth,
                    Items = new()
                    {
                        new AdaptiveTextBlock
                        {
                            Text = duration.ToDurationStringRussianCulture(),
                            Size = AdaptiveTextSize.Default
                        }
                    }
                },
                new()
                {
                    Items = new()
                    {
                        new AdaptiveTextBlock
                        {
                            Text = $"**{projectName}**",
                            Size = AdaptiveTextSize.Default,
                            Wrap = false
                        }
                    }
                }
            }
        };

    private static AdaptiveColumnSet CreateAdaptiveDescriptionRow(string description)
        =>
        new()
        {
            Spacing = AdaptiveSpacing.None,
            Columns = new()
            {
                new()
                {
                    Width = TimeColumnWidth
                },
                new()
                {
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = $"_{description}_",
                            Size = AdaptiveTextSize.Default,
                            Wrap = true
                        }
                    }
                }
            }
        };

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);

    private static string BuildText(IChatFlowContext<DateTimesheetFlowState> context)
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
                timesheet.Duration.ToDurationStringRussianCulture(true), context.EncodeTextWithStyle(timesheet.ProjectName, BotTextStyle.Bold));

            if (string.IsNullOrEmpty(timesheet.Description))
            {
                return row;
            }

            return row.Append(BotLine).AppendFormat(
                context.EncodeTextWithStyle(timesheet.Description, BotTextStyle.Italic));
        }
    }

    private static string BuildTelegramText(IChatFlowContext<DateTimesheetFlowState> context)
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
                timesheet.Duration.ToDurationStringRussianCulture(true), $"<b>{HttpUtility.HtmlEncode(timesheet.ProjectName)}</b>");

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

    private static decimal GetDurationSum(this DateTimesheetFlowState flowState)
        =>
        flowState.Timesheets?.Any() is true ? flowState.Timesheets.Sum(x => x.Duration) : default;
}