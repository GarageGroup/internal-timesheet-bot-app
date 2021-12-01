namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public bool Equals(ChatFlowAction<T> other)
        =>
        CodeComparer.Equals(Code, other.Code) &&
        FlowStateComparer.Equals(flowState, other.flowState) &&
        StepStateComparer.Equals(stepState, other.stepState);
}