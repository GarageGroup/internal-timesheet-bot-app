using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal sealed class UserStateProvider : IUserStateProvider
{
    private readonly UserState userState;
    private readonly IStatePropertyAccessor<UserDataJson?> userDataAccessor;
    private readonly IStatePropertyAccessor<FlowStateJson> flowStateAccessor;

    internal UserStateProvider(UserState userState)
    {
        this.userState = userState;
        userDataAccessor = userState.CreateProperty<UserDataJson?>("__userData");
        flowStateAccessor = userState.CreateProperty<FlowStateJson>("__userFlowState");
    }

    public async ValueTask<UserDataJson?> GetUserDataAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        =>
        await userDataAccessor.GetAsync(turnContext, static () => default, cancellationToken).ConfigureAwait(false);

    public async ValueTask<Unit> SaveUserDataAsync(ITurnContext turnContext, UserDataJson userData, CancellationToken cancellationToken)
    {
        await userDataAccessor.SetAsync(turnContext, userData, cancellationToken).ConfigureAwait(false);
        await flowStateAccessor.DeleteAsync(turnContext, cancellationToken).ConfigureAwait(false);

        return default;
    }

    public async ValueTask<FlowStateJson> GetFlowStateAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        =>
        await flowStateAccessor.GetAsync(turnContext, static () => default, cancellationToken).ConfigureAwait(false);

    public async ValueTask<Unit> SaveFlowStateAsync(ITurnContext turnContext, FlowStateJson flowState, CancellationToken cancellationToken)
    {
        await flowStateAccessor.SetAsync(turnContext, flowState, cancellationToken).ConfigureAwait(false);
        await userDataAccessor.DeleteAsync(turnContext, cancellationToken).ConfigureAwait(false);

        return default;
    }

    public async ValueTask<Unit> ClearAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        await userState.DeleteAsync(turnContext, cancellationToken).ConfigureAwait(false);
        return default;
    }
}