namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowResult<T>
{
    public static bool Equals(ChatFlowResult<T> left, ChatFlowResult<T> right)
        =>
        left.Equals(right);

    public static bool operator ==(ChatFlowResult<T> left, ChatFlowResult<T> right)
        =>
        left.Equals(right);

    public static bool operator !=(ChatFlowResult<T> left, ChatFlowResult<T> right)
        =>
        left.Equals(right) is false;

    public override bool Equals(object? obj)
        =>
        obj is ChatFlowResult<T> other && Equals(other);
}