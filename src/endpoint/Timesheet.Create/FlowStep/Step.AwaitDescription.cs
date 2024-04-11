using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitDescription(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmTimesheetApi crmTimesheetApi)
        =>
        chatFlow.ForwardValue(
            crmTimesheetApi.GetDescriptionTagsAsync)
        .AwaitText(
            static context => new(
                messageText: context.BuildDescriptionMessageText(),
                suggestions:
                [
                    [
                        new("Пропустить", string.Empty)
                    ]
                ])
            {
                SkipStep = context.FlowState.Description is not null
            },
            static (context, description) => string.IsNullOrEmpty(description) switch
            {
                true => "Описание пропущено",
                _ => $"Описание: {context.EncodeTextWithStyle(description, BotTextStyle.Bold)}"
            },
            static (flowState, description) => flowState with
            {
                Description = new(description.OrNullIfEmpty())
            });

    private static async ValueTask<ChatFlowJump<TimesheetCreateFlowState>> GetDescriptionTagsAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
    {
        if (context.IsNotTelegramChannel() || context.FlowState.Description is not null)
        {
            return context.FlowState;
        }

        await context.SetTypingStatusAsync(cancellationToken).ConfigureAwait(false);

        return context.FlowState with
        {
            DescriptionTags = await crmTimesheetApi.GetTagsAsync(context, cancellationToken).ConfigureAwait(false)
        };
    }

    private static string BuildDescriptionMessageText(this IChatFlowContext<TimesheetCreateFlowState> context)
    {
        const string DefaultMessageText = "Введите описание. Этот шаг можно пропустить";
        if (context.IsNotTelegramChannel() || context.FlowState.DescriptionTags?.Length is not > 0)
        {
            return DefaultMessageText;
        }

        var builder = new StringBuilder(DefaultMessageText).Append("\n\r");

        for (var i = 0; i < context.FlowState.DescriptionTags.Length; i++)
        {
            if (i > 0)
            {
                builder = builder.Append("\n\r");
            }

            var encodedTag = HttpUtility.HtmlEncode(context.FlowState.DescriptionTags[i]);
            builder = builder.Append("<code>").Append(encodedTag).Append("</code>");
        }

        return builder.ToString();
    }

    private static ValueTask<FlatArray<string>> GetTagsAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            static state => new TimesheetTagSetGetIn(
                userId: state.UserId,
                projectId: state.Project?.Id ?? default,
                minDate: state.Date.GetValueOrDefault().AddDays(-DescriptionTagDays),
                maxDate: state.Date.GetValueOrDefault()))
        .PipeValue(
            crmTimesheetApi.GetTagSetAsync)
        .OnFailure(
            failure => context.Logger.LogError(failure.SourceException, "GetTags failure: '{failureMessage}'", failure.FailureMessage))
        .Fold(
            static success => success.Tags,
            static _ => default);
}