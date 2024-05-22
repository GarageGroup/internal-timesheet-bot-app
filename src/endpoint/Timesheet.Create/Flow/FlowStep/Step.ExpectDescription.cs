using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ExpectDescriptionOrSkip(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmTimesheetApi crmTimesheetApi)
        =>
        chatFlow.ForwardValue(
            crmTimesheetApi.GetDescriptionTagsOrSkipAsync)
        .ExpectTextOrSkip(
            CreateDescriptionStepOption);

    private static async ValueTask<ChatFlowJump<TimesheetCreateFlowState>> GetDescriptionTagsOrSkipAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
    {
        if (context.FlowState.Description is not null)
        {
            return context.FlowState;
        }

        var typingTask = context.Api.SendChatActionAsync(BotChatAction.Typing, cancellationToken);
        var tagsTask = crmTimesheetApi.GetTagsAsync(context, cancellationToken);

        await Task.WhenAll(typingTask, tagsTask).ConfigureAwait(false);

        return context.FlowState with
        {
            DescriptionTags = tagsTask.Result
        };
    }

    private static Task<FlatArray<string>> GetTagsAsync(
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

    private static TextStepOption<TimesheetCreateFlowState>? CreateDescriptionStepOption(
        IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Description is not null)
        {
            return null;
        }

        return new(
            text: context.BuildDescriptionMessageText(),
            forward: ForwardDescription)
        {
            Suggestions =
            [
                [
                    new(context.Localizer[Skip], string.Empty)
                ]
            ]
        };

        Result<TimesheetCreateFlowState, ChatRepeatState> ForwardDescription(string description)
            =>
            context.FlowState with
            {
                Description = new(description)
            };
    }

    private static string BuildDescriptionMessageText(this IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.DescriptionTags.IsEmpty)
        {
            return context.Localizer[DescriptionMessageText];
        }

        var builder = new StringBuilder(context.Localizer[DescriptionMessageText]);

        foreach (var tag in context.FlowState.DescriptionTags)
        {
            var encodedTag = HttpUtility.HtmlEncode(tag);
            builder = builder.Append("\n\r<code>").Append(encodedTag).Append("</code>");
        }

        return builder.ToString();
    }
}