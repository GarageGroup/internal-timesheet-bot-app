using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public object? StepStateOrThrow()
        =>
        InnerStepStateOrThrow(
            CreateUnexpectedStepStateActionCodeException);

    public object? StepStateOrThrow(Func<Exception> exceptionFactory)
        =>
        InnerStepStateOrThrow(
            exceptionFactory ?? throw new ArgumentNullException(nameof(exceptionFactory)));

    private object? InnerStepStateOrThrow(Func<Exception> exceptionFactory)
        =>
        Code is ChatFlowActionCode.AwaitingAndRetry ? stepState : throw exceptionFactory.Invoke();

    private static InvalidOperationException CreateUnexpectedStepStateActionCodeException()
        =>
        new("The flow action is expected to have AwaitingAndRetry code.");
}