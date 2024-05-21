using System;
using System.Globalization;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ExpectDurationOrSkip(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.ExpectValueOrSkip(
            CreateDurationStepOption);

    private static ValueStepOption<TimesheetCreateFlowState, decimal>? CreateDurationStepOption(
        IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Duration > 0)
        {
            return null;
        }

        return new(
            text: context.Localizer[DurationMessageText],
            parse: ParseDurationOrRepeat,
            forward: ForwardDuration)
        {
            Suggestions = DurationSuggestions
        };

        Result<decimal, ChatRepeatState> ParseDurationOrRepeat(string text)
        {
            foreach (var culture in DurationParserCultures)
            {
                if (decimal.TryParse(text, NumberStyles.Number, culture, out var value))
                {
                    return context.ValidateDurationOrRepeat(value);
                }
            }

            return ChatRepeatState.From(context.Localizer[InvalidDurationText]);
        }

        Result<TimesheetCreateFlowState, ChatRepeatState> ForwardDuration(decimal duration)
            =>
            context.FlowState with
            {
                Duration = duration
            };
    }

    private static Result<decimal, ChatRepeatState> ValidateDurationOrRepeat(
        this IChatFlowContext<TimesheetCreateFlowState> context, decimal value)
        =>
        value switch
        {
            not > 0 => ChatRepeatState.From(context.Localizer[TooShortDurationText]),
            not <= MaxDurationValue => ChatRepeatState.From(context.Localizer.GetString(TooLongDurationTemplate, MaxDurationValue)),
            _ => Result.Success(value)
        };
}