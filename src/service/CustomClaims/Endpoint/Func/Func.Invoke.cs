using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using GarageGroup.Internal.Timesheet.Inner;

namespace GarageGroup.Internal.Timesheet;

partial class ProvideClaimsFunc
{
    public ValueTask<Result<ProvideClaimsOut, Failure<ProvideClaimsFailureCode>>> InvokeAsync(
        ProvideClaimsIn input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            ValidateInput)
        .Forward(
            BuildUserGetInput)
        .ForwardValue(
            dataverseApi.GetEntityAsync<UserJson>,
            failure => failure.MapFailureCode(MapFailureCode))
        .MapSuccess(
            systemUserId => new ProvideClaimsOut
            {
                Data = new AuthenticationEventResponseData
                {
                    Actions = 
                    [
                        new TokenIssuanceAction
                        {
                            Claims = new CustomClaims
                            {
                                SystemUserId = systemUserId.Value.Id
                            }
                        }
                    ] 
                }
            });

    private static Result<ProvideClaimsIn, Failure<ProvideClaimsFailureCode>> ValidateInput(ProvideClaimsIn input)
    {
        if (input.Data.AuthenticationContext.User.Id == Guid.Empty)
        {
            return Failure.Create(ProvideClaimsFailureCode.InvalidQuery, "The Azure Active Directory user is not specified");
        }

        return input;
    }

    private static Result<DataverseEntityGetIn, Failure<ProvideClaimsFailureCode>> BuildUserGetInput(
        ProvideClaimsIn input)
        => 
        UserJson.BuildGetInput(input.Data.AuthenticationContext.User.Id);
    
    private static ProvideClaimsFailureCode MapFailureCode(DataverseFailureCode failureCode)
        => 
        failureCode switch
        {
            _ => ProvideClaimsFailureCode.Unknown      
        };
}