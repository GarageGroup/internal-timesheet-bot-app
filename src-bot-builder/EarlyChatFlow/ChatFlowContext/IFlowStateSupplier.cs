using System;

namespace GGroupp.Infra.Bot.Builder;

public interface IFlowStateSupplier<T>
{
    public T FlowState { get; }

    ChatFlowAction<T> NextSameAction()
        =>
        new(FlowState);

    ChatFlowAction<T> NextSameAction(Unit _)
        =>
        new(FlowState);

    ChatFlowAction<T> CancelAction()
        =>
        new(isCanceling: true);

    ChatFlowAction<T> CancelAction(Unit _)
        =>
        new(isCanceling: true);

    ChatFlowAction<T> InterruptAction()
        =>
        new(isCanceling: true);

    ChatFlowAction<T> InterruptAction(Unit _)
        =>
        new(isCanceling: true);
}