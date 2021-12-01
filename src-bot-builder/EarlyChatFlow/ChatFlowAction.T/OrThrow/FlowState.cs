using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public T FlowStateOrThrow()
        =>
        InnerFlowStateOrThrow(
            CreateUnexpectedFlowStateActionCodeException);

    public T FlowStateOrThrow(Func<Exception> exceptionFactory)
        =>
        InnerFlowStateOrThrow(
            exceptionFactory ?? throw new ArgumentNullException(nameof(exceptionFactory)));

    private T InnerFlowStateOrThrow(Func<Exception> exceptionFactory)
        =>
        Code is ChatFlowActionCode.Next ? flowState : throw exceptionFactory.Invoke();

    private static InvalidOperationException CreateUnexpectedFlowStateActionCodeException()
        =>
        new("The flow action is expected to have Next code.");
}