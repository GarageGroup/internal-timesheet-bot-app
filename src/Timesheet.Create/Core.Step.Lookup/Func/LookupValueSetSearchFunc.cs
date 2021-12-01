using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

public delegate ValueTask<Result<LookupValueSetSeachOut, Failure<Unit>>> LookupValueSetSearchFunc<TFlowIn>(
    TFlowIn flowState, LookupValueSetSeachIn searchInput, CancellationToken cancellationToken);