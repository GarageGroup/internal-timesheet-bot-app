namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public static ChatFlowAction<T> AwaitAndRetry(object? stepState) => new(stepState);
}