namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlowAction
{
    public static ChatFlowAction<T> Next<T>(T value) => new(value);
}