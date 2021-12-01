namespace GGroupp.Infra.Bot.Builder;

internal readonly record struct ChatFlowStepCacheJson<T>
{
    public T? FlowState { get; init; }

    public object? StepState { get; init; }
}