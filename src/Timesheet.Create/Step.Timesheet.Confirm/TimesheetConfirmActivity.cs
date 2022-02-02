using System;
using System.Globalization;
using System.Text;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetConfirmActivity
{
    private const string QuestionText = "Списать время?";

    private const string ActionCreateText = "Списать";

    private const string ActionCancelText = "Отменить";

    private const string CanceledText = "Списание времени было отменено";

    private static readonly Guid ActionCreateId, ActionCancelId;

    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetConfirmActivity()
    {
        ActionCreateId = Guid.Parse("816e1f34-747f-4a8b-b7e6-cb62e41adae0");
        ActionCancelId = Guid.Parse("651b51de-9347-40ea-81ba-2c14eab20d4a");
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");
    }

    internal static ChatFlowJump<TimesheetCreateFlowStateJson> GetConfirmationResult(this IChatFlowContext<TimesheetCreateFlowStateJson> context)
        =>
        context.GetCardActionValueOrAbsent().Fold(
            actionId => actionId switch
            {
                _ when actionId == ActionCreateId => ChatFlowJump.Next(context.FlowState),
                _ when actionId == ActionCancelId => ChatFlowBreakState.From(CanceledText),
                _ => context.RepeatSameStateJump<TimesheetCreateFlowStateJson>()
            },
            context.RepeatSameStateJump<TimesheetCreateFlowStateJson>);

    internal static IActivity CreateActivity(IChatFlowContext<TimesheetCreateFlowStateJson> context)
        =>
        context.IsCardSupported() switch
        {
            true => context.CreateCardSupportedConfirmationActivity(),
            _ => context.CreateSimpleConfirmationActivity()
        };

    private static IActivity CreateSimpleConfirmationActivity(this IChatFlowContext<TimesheetCreateFlowStateJson> context)
    {
        var flowState = context.FlowState;

        var summaryBuilder = context.CreateSummaryTextBuilder(flowState);
        if (string.IsNullOrEmpty(flowState.Description) is false)
        {
            var description = context.EncodeText(flowState.Description);
            summaryBuilder = summaryBuilder.AppendLine(context).AppendRow(context, "Описание", description);
        }

        var card = new HeroCard
        {
            Title = QuestionText,
            Buttons = context.CreateCardActions()
        };

        return MessageFactory.Attachment(card.ToAttachment(), summaryBuilder.ToString());
    }

    private static IActivity CreateCardSupportedConfirmationActivity(this IChatFlowContext<TimesheetCreateFlowStateJson> context)
        =>
        new HeroCard
        {
            Title = QuestionText,
            Subtitle = context.CreateSummaryTextBuilder(context.FlowState).ToString(),
            Text = context.FlowState.Description,
            Buttons = context.CreateCardActions()
        }
        .ToAttachment()
        .ToActivity();

    private static CardAction[] CreateCardActions(this ITurnContext turnContext)
        =>
        new CardAction[]
        {
            new(ActionTypes.PostBack)
            {
                Title = ActionCreateText,
                Text = ActionCreateText,
                Value = turnContext.BuildCardActionValue(ActionCreateId)
            },
            new(ActionTypes.PostBack)
            {
                Title = ActionCancelText,
                Text = ActionCancelText,
                Value = turnContext.BuildCardActionValue(ActionCancelId)
            }
        };

    private static StringBuilder CreateSummaryTextBuilder(this ITurnContext turnContext, TimesheetCreateFlowStateJson flowStateJson)
        =>
        Pipeline.Pipe(
            new StringBuilder())
        .AppendRow(
            turnContext, "Проект", turnContext.EncodeText(flowStateJson.ProjectName))
        .AppendLine(
            turnContext)
        .AppendRow(
            turnContext, "Дата", flowStateJson.Date.ToString("dd MMMM yyyy", RussianCultureInfo))
        .AppendLine(
            turnContext)
        .AppendRow(
            turnContext, "Время", flowStateJson.ValueHours.ToString("G", RussianCultureInfo));

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
}