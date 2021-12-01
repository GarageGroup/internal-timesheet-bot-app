namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowResult<T>
{
    public static ChatFlowResult<T> Cancel() => new(ChatFlowResultCode.Canceling);
}