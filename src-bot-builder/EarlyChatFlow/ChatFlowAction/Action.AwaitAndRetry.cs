namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlowAction
{
    public static ChatFlowAction<T> AwaitAndRetry<T>(object? stepState) => new(stepState);
}