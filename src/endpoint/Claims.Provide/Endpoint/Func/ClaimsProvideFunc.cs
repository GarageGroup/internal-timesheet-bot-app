using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class ClaimsProvideFunc(IDataverseEntityGetSupplier dataverseApi) : IClaimsProvideFunc
{
    private static ClaimsProvideFailureCode MapFailureCodeWhenSearchingForUser(DataverseFailureCode failureCode)
        => 
        failureCode switch
        {
            DataverseFailureCode.RecordNotFound => ClaimsProvideFailureCode.UserNotFound,
            _ => ClaimsProvideFailureCode.Unknown
        };
}