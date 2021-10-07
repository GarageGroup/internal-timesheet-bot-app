#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Internal.Timesheet.Bot
{
    public delegate Task<FlowDialogAction<TNextState>> FlowDialogStep<in TState, TNextState>(
        IFlowStateTurnContext<TState> turnContext, CancellationToken cancellationToken)
        where TState : class
        where TNextState : class;
}