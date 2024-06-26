using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public enum ClaimsProvideFailureCode
{
    Unknown,
    
    InvalidQuery,
    
    [Problem(FailureStatusCode.NotFound, "System user was not found")]
    UserNotFound
}
