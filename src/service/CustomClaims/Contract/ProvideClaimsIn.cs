using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class ProvideClaimsIn
{
    public ProvideClaimsIn([JsonBodyIn]AuthenticationEventData data)
        => 
        Data = data;
    
    public AuthenticationEventData Data { get; init; }    
}