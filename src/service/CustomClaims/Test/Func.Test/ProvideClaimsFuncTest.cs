using System;
using System.Threading;
using GarageGroup.Infra;
using GarageGroup.Internal.Timesheet.Inner;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.CustomClaims.Test.Func.Test;

public static partial class ProvideClaimsFuncTest
{
    private static readonly ProvideClaimsIn SomeInput 
        = 
        new (new AuthenticationEventData
        {
            AuthenticationContext = new AuthenticationContext
            {
                User = new User
                {
                    Id = Guid.Parse("5b228f06-d220-4006-844a-374df853108d")
                }
            }
        });

    private static readonly DataverseEntityGetOut<UserJson> SomeGetUserResult
        =
        new(new UserJson
        {
            Id = Guid.Parse("5b228f06-d220-4006-844a-374df853108d")
        });
    
    private static Mock<IDataverseEntityGetSupplier> BuildDataverseMock(
        in Result<DataverseEntityGetOut<UserJson>, Failure<DataverseFailureCode>> getUserResult)
    {
        var mock = new Mock<IDataverseEntityGetSupplier>();

        mock
            .Setup(x => x.GetEntityAsync<UserJson>(It.IsAny<DataverseEntityGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getUserResult);
        
        return mock;
    }
}