using AdaptiveCards;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetSetGetActivity
{
    private const string TimeColumnWidth = "45px";

    private static readonly string LineSeparator;

    static TimesheetSetGetActivity()
        =>
        LineSeparator = new('-', 50);

    internal static IActivity CreateActivity(IChatFlowContext<DateTimesheetFlowState> context)
    {
        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return MessageFactory.Text($"Нет списаний времени на {context.FlowState.Date.ToStringRussianCulture()}");
        }

        if (context.IsCardSupported())
        {
            return CreateAdaptiveCardActivity(context);
        }

        return CreateTextActivity(context);
    }

    private static IActivity CreateTextActivity(IChatFlowContext<DateTimesheetFlowState> context)
    {
        var textBuilder = new StringBuilder(BuildHeader(context.FlowState));
        if (context.FlowState.Timesheets is null)
        {
            return MessageFactory.Text(textBuilder.ToString());
        }

        foreach (var timesheetText in context.FlowState.Timesheets.Select(BuildTimesheetText))
        {
            textBuilder.AppendBotLine().Append(LineSeparator).AppendBotLine().Append(timesheetText);
        }

        return MessageFactory.Text(textBuilder.ToString());

        StringBuilder BuildTimesheetText(TimesheetJson timesheet)
        {
            var row = new StringBuilder().AppendFormat(
                "{0,-10}{1}",
                timesheet.Duration.ToDurationStringRussianCulture(true),
                context.EncodeTextWithStyle(timesheet.ProjectName, BotTextStyle.Bold));

            var encodedDescription = context.EncodeTextWithStyle(timesheet.Description, BotTextStyle.Italic);
            if (string.IsNullOrEmpty(encodedDescription) is false)
            {
                row.AppendBotLine().Append(encodedDescription);
            }

            return row;
        }

        static string BuildHeader(DateTimesheetFlowState flowState)
            =>
            string.Format(
                "{0,-10}**Всего {1}**",
                flowState.GetDurationSum().ToDurationStringRussianCulture(true),
                flowState.Date.ToStringRussianCulture());
    }

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

    private static List<AdaptiveElement> CreateAdaptiveBody(IChatFlowContext<DateTimesheetFlowState> context)
    {
        var adaptiveElements = new List<AdaptiveElement>
        {
            CreateAdaptiveTimesheetRow(context.FlowState.GetDurationSum(), $"Всего {context.FlowState.Date.ToStringRussianCulture()}")
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

    private static StringBuilder AppendBotLine(this StringBuilder builder)
        =>
        builder.Append("\n\r");

    private static decimal GetDurationSum(this DateTimesheetFlowState flowState)
        =>
        flowState.Timesheets?.Sum(x => x.Duration) ?? default;
}