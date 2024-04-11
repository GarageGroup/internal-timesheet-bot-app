﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> DeleteTimesheet(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SetTypingStatus(
            CreateInfoActivity)
        .ForwardValue(
            timesheetApi.DeleteTimesheetsAsync);

    private static ValueTask<ChatFlowJump<TimesheetDeleteFlowState>> DeleteTimesheetsAsync(
        this ICrmTimesheetApi crmTimesheetApi,
        IChatFlowContext<TimesheetDeleteFlowState> context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .On(
            (context, token) => context.DeleteActivityAsync(context.Activity.Id, cancellationToken))
        .Pipe(
            static context => context.FlowState.TimesheetIds.Map(BuildDeletionInput))
        .PipeParallelValue(
            crmTimesheetApi.DeleteAsync)
        .Map(
            _ => context.FlowState,
            static failure => failure.SourceException.ToChatFlowBreakState("Не удалось удалить одну или несколько записей", failure.FailureMessage))
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetDeleteFlowState>);

    private static IActivity? CreateInfoActivity(IChatFlowContext<TimesheetDeleteFlowState> context)
        =>
        context.FlowState.TimesheetIds.Length switch
        {
            1 => MessageFactory.Text("Выполняется удаление списания времени"),
            _ => MessageFactory.Text("Выполняется удаление списаний времени")
        };

    private static TimesheetDeleteIn BuildDeletionInput(Guid timesheetId)
        =>
        new(timesheetId);
}