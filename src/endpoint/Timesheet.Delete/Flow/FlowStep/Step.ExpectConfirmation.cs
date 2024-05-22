using System;
using System.Globalization;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetDeleteResource;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> ExpectDeletionConfirmation(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow)
        =>
        chatFlow.ExpectConfirmationOrSkip(
            CreateTimesheetConfirmationOption);

    private static ConfirmationCardOption<TimesheetDeleteFlowState> CreateTimesheetConfirmationOption(
        IChatFlowContext<TimesheetDeleteFlowState> context)
        =>
        new()
        {
            Entity = context.CreateTimesheetCardOption(),
            Keyboard = new(
                confirmButtonText: context.Localizer[DeletionConfirmButton],
                cancelButtonText: context.Localizer[DeletionCancelButton],
                cancelText: context.Localizer[DeletionCancelText])
        };

    private static EntityCardOption CreateTimesheetCardOption(
        this IChatFlowContext<TimesheetDeleteFlowState> context)
    {
        var dateText = context.FlowState.Date.ToString("d MMMM yyyy", context.User.Culture);
        var durationText = context.FlowState.Duration.ToString("G", CultureInfo.InvariantCulture);

        return new(context.Localizer[DeletionHeaderText])
        {
            FieldValues =
            [
                new(context.FlowState.ProjectTypeDisplayName.OrEmpty(), context.FlowState.ProjectName),
                new(context.Localizer[DateFieldName], dateText),
                new(context.Localizer[DurationFieldName], durationText + context.Localizer[HourSymbol]),
                new(string.Empty, context.FlowState.Description)
            ]
        };
    }
}