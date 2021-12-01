using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowResult<T>
{
    public T FlowStateOrThrow()
        =>
        InnerFlowStateOrThrow(
            CreateExpectedPresentFlowStateException);

    public T FlowStateOrThrow(Func<Exception> exceptionFactory)
        =>
        InnerFlowStateOrThrow(
            exceptionFactory ?? throw new ArgumentNullException(nameof(exceptionFactory)));

    private T InnerFlowStateOrThrow(Func<Exception> exceptionFactory)
        =>
        Code is ChatFlowResultCode.Running ? flowState : throw exceptionFactory.Invoke();

    private static InvalidOperationException CreateExpectedPresentFlowStateException()
        =>
        new("The flow step result is expected to have a value.");
}