using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal interface IUserStateProvider
{
    ValueTask<UserDataJson?> GetUserDataAsync(ITurnContext turnContext, CancellationToken cancellationToken);

    ValueTask<Unit> SaveUserDataAsync(ITurnContext turnContext, UserDataJson userData, CancellationToken cancellationToken);

    ValueTask<FlowStateJson> GetFlowStateAsync(ITurnContext turnContext, CancellationToken cancellationToken);

    ValueTask<Unit> SaveFlowStateAsync(ITurnContext turnContext, FlowStateJson flowState, CancellationToken cancellationToken);

    ValueTask<Unit> ClearAsync(ITurnContext turnContext, CancellationToken cancellationToken);
}