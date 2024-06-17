using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class ClaimsProvideIn
{
    public ClaimsProvideIn([JsonBodyIn]AuthenticationEventData? data)
        => 
        Data = data;
    
    public AuthenticationEventData? Data { get; init; }    
}