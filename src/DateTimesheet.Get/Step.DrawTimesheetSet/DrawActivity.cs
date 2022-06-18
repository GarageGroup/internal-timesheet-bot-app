using AdaptiveCards;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace GGroupp.Internal.Timesheet;

internal static class DrawActivity
{
    private const string TimeColumnWidth = "45px";

    private static readonly string LineSeparator;

    static DrawActivity()
        =>
        LineSeparator = new('-', 50);

    internal static IActivity CreateActivity(IChatFlowContext<DateTimesheetFlowState> context)
    {
        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return context.CreateTextActivity($"Нет списаний времени за {context.FlowState.Date.ToStringRussianCulture()}");
        }

        if (context.IsCardSupported())
        {
            return CreateAdaptiveCardActivity(context);
        }

        if (context.IsTelegramChannel())
        {
            return context.Pipe(BuildTelegramText).Pipe(CreateTelegramTextActivity);
        }

        var text = BuildText(context);
        return MessageFactory.Text(text);
    }

    private static IActivity CreateTextActivity(this ITurnContext turnContext, string text)
        =>
        turnContext.IsNotTelegramChannel() ? MessageFactory.Text(text) : CreateTelegramTextActivity(text);

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

    private static IActivity CreateTelegramTextActivity(string text)
    {
        var channelData = new TelegramChannelData(
            parameters: new TelegramParameters(text)
            {
                ParseMode = TelegramParseMode.Html,
                ReplyMarkup = new TelegramReplyKeyboardRemove()
            });

        var activity = MessageFactory.Text(default);
        activity.ChannelData = channelData.ToJObject();

        return activity;
    }

    private static List<AdaptiveElement> CreateAdaptiveBody(IChatFlowContext<DateTimesheetFlowState> context)
    {
        var adaptiveElements = new List<AdaptiveElement>
        {
            CreateAdaptiveTimesheetRow(context.FlowState.GetDurationSum(), $"Всего за {context.FlowState.Date.ToStringRussianCulture()}")
        };

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
        const string botLine = "\n\r\n\r";
        var flowState = context.FlowState;

        var textBuilder = new StringBuilder().AppendRow(
            flowState.GetDurationSum().ToDurationStringRussianCulture(true),
            context.EncodeTextWithStyle($"Всего за {context.FlowState.Date.ToStringRussianCulture()}", BotTextStyle.Bold));

        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return textBuilder.ToString();
        }

        foreach (var timesheetText in context.FlowState.Timesheets.Select(BuildTimesheetText))
        {
            textBuilder.Append(botLine).Append(LineSeparator).Append(botLine).Append(timesheetText);
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

            return row.Append(botLine).AppendFormat(
                context.EncodeTextWithStyle(timesheet.Description, BotTextStyle.Italic));
        }
    }

    private static string BuildTelegramText(IChatFlowContext<DateTimesheetFlowState> context)
    {
        const string botLine = "\n\r";
        var flowState = context.FlowState;

        var textBuilder = new StringBuilder().AppendRow(
            flowState.GetDurationSum().ToDurationStringRussianCulture(true), $"<b>Всего за {flowState.Date.ToStringRussianCulture()}</b>");

        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return textBuilder.ToString();
        }

        foreach (var timesheetText in context.FlowState.Timesheets.Select(BuildTimesheetText))
        {
            textBuilder.Append(botLine).Append(LineSeparator).Append(botLine).Append(timesheetText);
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

            return row.Append(botLine).Append(
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