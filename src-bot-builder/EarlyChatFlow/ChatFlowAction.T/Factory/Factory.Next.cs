namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public static ChatFlowAction<T> Next(T value) => new(value);
}