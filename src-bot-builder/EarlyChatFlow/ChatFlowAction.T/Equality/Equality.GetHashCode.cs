using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public override int GetHashCode()
        =>
        Code is ChatFlowActionCode.Next ? GetNextHashCode() :
        Code is ChatFlowActionCode.AwaitingAndRetry ? GetAwaitingHashCode() :
        GetInterruptHashCode();

    private int GetNextHashCode()
        =>
        flowState is not null
            ? HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code), FlowStateComparer.GetHashCode(flowState))
            : HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code));

    private int GetAwaitingHashCode()
        =>
        stepState is not null
            ? HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code), StepStateComparer.GetHashCode(stepState))
            : HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code));

    private int GetInterruptHashCode()
        =>
        HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code));
}