namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowResult<T>
{
    public bool Equals(ChatFlowResult<T> other)
        =>
        CodeComparer.Equals(Code, other.Code) &&
        FlowStateComparer.Equals(flowState, other.flowState);
}