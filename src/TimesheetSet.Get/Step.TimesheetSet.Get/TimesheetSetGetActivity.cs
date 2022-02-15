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


        var text = new StringBuilder();
        var isTelegram = context.IsTelegramChannel();

        text.Append("**").Append(context.FlowState.Timesheets.Sum(x => x.Duration).ToString("#,##0.##") + "ч Всего списаний").Append(':').Append("**");

        foreach (var timesheet in context.FlowState.Timesheets)
        {
            if (isTelegram)
            {
                text.Append("\n\r---\n\r");
                text.Append(context.CreateSummaryTextBuilder(timesheet));
            }
        }



        return MessageFactory.Text(text.ToString());
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
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock
                    {
                        Text = "",
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Medium
                    },
                    new AdaptiveFactSet
                    {
                        Facts = GetAdaptiveFacts(timesheets)
                    }
                }
            }
        }.ToActivity();

        return result;
    }

    private static List<AdaptiveFact> GetAdaptiveFacts(IReadOnlyCollection<TimesheetSetItemGetOut> timesheets) { 
        var result = new List<AdaptiveFact>();

        foreach (var timesheet in timesheets)
        {
            result.Add(new AdaptiveFact { Title = timesheet.Duration.ToString("#,##0.##") + "ч", Value = timesheet.ProjectName });

            if (string.IsNullOrEmpty(timesheet.Description) is false) {
                result.Add(new AdaptiveFact { Title = "", Value = timesheet.Description });
            }
        }

        return result;
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