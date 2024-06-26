using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using GarageGroup.Internal.Timesheet.Inner;

namespace GarageGroup.Internal.Timesheet;

partial class ClaimsProvideFunc
{
    public ValueTask<Result<ClaimsProvideOut, Failure<ClaimsProvideFailureCode>>> InvokeAsync(
        ClaimsProvideIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateUserId)
        .MapSuccess(
            UserJson.BuildGetInput)
        .ForwardValue(
            dataverseApi.GetEntityAsync<UserJson>,
            static failure => failure.MapFailureCode(MapFailureCodeWhenSearchingForUser))
        .MapSuccess(
            systemUser => new ClaimsProvideOut
            {
                Data = new AuthenticationEventResponseData
                {
                    Actions = 
                    [
                        new TokenIssuanceAction
                        {
                            Claims = new Claims
                            {
                                CorrelationId = input.Data!.AuthenticationContext!.CorrelationId,
                                SystemUserId = systemUser.Value.Id
                            }
                        }
                    ] 
                }
            });

    private static Result<Guid, Failure<ClaimsProvideFailureCode>> ValidateUserId(ClaimsProvideIn input)
    {
        if (input.Data?.AuthenticationContext?.User is null)
        {
            return Failure.Create(ClaimsProvideFailureCode.InvalidQuery, "The Azure Active Directory user is not specified");
        }

        return input.Data.AuthenticationContext.User.Id;
    }
}
