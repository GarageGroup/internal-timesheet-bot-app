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
    internal static IActivity CreateActivity(IChatFlowContext<TimesheetSetGetOut> context)
    {
        if (context.FlowState.Timesheets.Count is not > 0)
        {
            return MessageFactory.Text("Нет списаний времени на указанную дату");
        }

        if (context.IsCardSupported())
        {
            return context.CreateAdaptiveCardActivity(context.FlowState.Timesheets);
        }

        if (context.IsTelegramChannel())
        {
            var text = new StringBuilder();

            text.Append("**").Append(context.FlowState.Timesheets.Sum(x => x.Duration).ToString("#,##0.##") + "ч Всего списаний").Append(':').Append("**");

            foreach (var timesheet in context.FlowState.Timesheets)
            {
                text.Append("\n\r---\n\r");
                text.Append(context.CreateSummaryTextBuilder(timesheet));
            }

            return MessageFactory.Text(text.ToString());
        }

        return MessageFactory.Text("При поиске списаний произошла ошибка. Не опознан канал связи. Обратитесь к администратору");
    }

    internal static StringBuilder CreateSummaryTextBuilder(this ITurnContext turnContext, TimesheetSetItemGetOut timesheet)
    {
        var result = new StringBuilder()
        .AppendRow(
            turnContext, string.Empty, timesheet.Duration.ToString("#,##0.##") + "ч")
        .AppendRow(
            turnContext, string.Empty, turnContext.EncodeText(timesheet.ProjectName))
        .AppendLine(
            turnContext);

        if (string.IsNullOrEmpty(timesheet.Description) is false)
        {
            result.AppendRow(
                turnContext, string.Empty, timesheet.Description);
        }

        return result;
    }

    private static IActivity CreateAdaptiveCardActivity(
        this ITurnContext context, IReadOnlyCollection<TimesheetSetItemGetOut> timesheets)
    {
        var result = new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.GetAdaptiveSchemaVersion())
            {
                Body = GetAdaptiveElements(timesheets)
            }
        }.ToActivity();

        return result;
    }

    private static List<AdaptiveElement> GetAdaptiveElements(IReadOnlyCollection<TimesheetSetItemGetOut> timesheets)
    {
        var adaptiveElements = new List<AdaptiveElement>
        {
            GetAdaptiveTimesheetRow(timesheets.Sum(x => x.Duration), "**", "Всего списаний", AdaptiveTextSize.ExtraLarge, AdaptiveSpacing.Default)
        };

        foreach (var timesheet in timesheets)
        {
            adaptiveElements.Add(GetAdaptiveTimesheetRow(timesheet.Duration, "**", timesheet.ProjectName, AdaptiveTextSize.Default, AdaptiveSpacing.Default));

            if (string.IsNullOrEmpty(timesheet.Description) is not true)
            {
                adaptiveElements.Add(GetAdaptiveTimesheetRow(null, "_", timesheet.Description, AdaptiveTextSize.Default, AdaptiveSpacing.None, true));
            }
        }

        return adaptiveElements;
    }

    private static AdaptiveColumnSet GetAdaptiveTimesheetRow(decimal? duration,
        string style,
        string description,
        AdaptiveTextSize size,
        AdaptiveSpacing spacing,
        bool wrap = false)
    {
        var timeColumn = new AdaptiveColumn
        {
            Width = "55px"
        };

        var row = new AdaptiveColumnSet
        {
            Spacing = spacing
        };

        if (duration is not null)
        {
            timeColumn.Items.Add(new AdaptiveTextBlock
            {
                Text = "**" + duration?.ToString("#,##0.##") + "ч" + "**",
                Size = size,
            });
        }
        row.Columns.Add(timeColumn);

        var projectColumn = new AdaptiveColumn();
        projectColumn.Items.Add(new AdaptiveTextBlock
        {
            Text = style + description + style,
            Size = size,
            Wrap = wrap
        });
        row.Columns.Add(projectColumn);

        return row;
    }

    private static StringBuilder AppendRow(this StringBuilder builder, ITurnContext turnContext, string fieldName, string? fieldValue)
    {
        if (string.IsNullOrEmpty(fieldName) is false)
        {
            if (turnContext.IsTelegramChannel())
            {
                _ = builder.Append("**").Append(fieldName).Append(':').Append("**");
            }
            else
            {
                _ = builder.Append(fieldName).Append(':');
            }
        }

        if (string.IsNullOrEmpty(fieldValue) is false)
        {
            _ = builder.Append(' ').Append(fieldValue);
        }

        return builder;
    }

    private static StringBuilder AppendLine(this StringBuilder builder, ITurnContext turnContext)
        =>
        builder.Append(
            turnContext.IsMsteamsChannel() ? "<br>" : "\n\r\n\r");

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);
}