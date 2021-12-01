using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowResult<T>
{
    public override int GetHashCode()
        =>
        Code is ChatFlowResultCode.Running ? PresentFlowStateHashCode() : AbsentFlowStateHashCode();

    private int PresentFlowStateHashCode()
        =>
        flowState is not null
            ? HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code), FlowStateComparer.GetHashCode(flowState))
            : HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code));

    private int AbsentFlowStateHashCode()
        =>
        HashCode.Combine(EqualityContract, CodeComparer.GetHashCode(Code));
}