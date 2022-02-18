using AdaptiveCards;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.FormattableString;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetSetGetActivity
{
    private const string TimeColumnWith = "55px";

    internal static IActivity CreateActivity(IChatFlowContext<DateTimesheetFlowState> context)
    {
        if (context.FlowState.Timesheets?.Count is not > 0)
        {
            return MessageFactory.Text("Нет списаний времени на указанную дату");
        }

        if (context.IsCardSupported())
        {
            return CreateAdaptiveCardActivity(context);
        }

        return CreateTextActivity(context);
    }

    private static IActivity CreateTextActivity(IChatFlowContext<DateTimesheetFlowState> context)
    {
        var text = new StringBuilder();
        var headerText = Invariant($"{context.FlowState.GetDurationSum():#,##0.##}ч Всего списаний {context.FlowState.Date:dd.MM.yyyy}:");

        if (context.IsTelegramChannel())
        {
            text.Append("**").Append(headerText).Append("**");
        }
        else
        {
            text.Append(headerText);
        }

        if (context.FlowState.Timesheets is null)
        {
            return MessageFactory.Text(text.ToString());
        }

        foreach (var timesheetText in context.FlowState.Timesheets.Select(BuildTimesheetText))
        {
            text.Append("\n\r---\n\r").Append(timesheetText);
        }

        return MessageFactory.Text(text.ToString());

        static string BuildTimesheetText(TimesheetJson timesheet)
            =>
            Invariant($"{timesheet.Duration:#,##0.##}ч\n\r{timesheet.Description}");
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
        var headerText = Invariant($"Всего списаний {context.FlowState.Date:dd.MM.yyyy}");
        var adaptiveElements = new List<AdaptiveElement>
        {
            CreateAdaptiveTimesheetRow(context.FlowState.GetDurationSum(), headerText, AdaptiveTextSize.ExtraLarge)
        };

        if (context.FlowState.Timesheets is null)
        {
            return adaptiveElements;
        }

        foreach (var timesheet in context.FlowState.Timesheets)
        {
            var timesheetRow = CreateAdaptiveTimesheetRow(timesheet.Duration, timesheet.ProjectName, AdaptiveTextSize.Default);
            adaptiveElements.Add(timesheetRow);

            if (string.IsNullOrEmpty(timesheet.Description) is false)
            {
                var descriptionRow = CreateAdaptiveDescriptionRow(timesheet.Description);
                adaptiveElements.Add(descriptionRow);
            }
        }

        return adaptiveElements;
    }

    private static AdaptiveColumnSet CreateAdaptiveTimesheetRow(decimal duration, string? projectName, AdaptiveTextSize size)
        =>
        new()
        {
            Spacing = AdaptiveSpacing.Default,
            Columns = new()
            {
                new()
                {
                    Width = TimeColumnWith,
                    Items = new()
                    {
                        new AdaptiveTextBlock
                        {
                            Text = Invariant($"**{duration:#,##0.##}ч**"),
                            Size = size
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
                            Size = size,
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
                    Width = TimeColumnWith
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

    private static decimal GetDurationSum(this DateTimesheetFlowState flowState)
        =>
        flowState.Timesheets?.Sum(x => x.Duration) ?? default;

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);
}