namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public static bool Equals(ChatFlowAction<T> left, ChatFlowAction<T> right)
        =>
        left.Equals(right);

    public static bool operator ==(ChatFlowAction<T> left, ChatFlowAction<T> right)
        =>
        left.Equals(right);

    public static bool operator !=(ChatFlowAction<T> left, ChatFlowAction<T> right)
        =>
        left.Equals(right) is false;

    public override bool Equals(object? obj)
        =>
        obj is ChatFlowAction<T> other && Equals(other);
}