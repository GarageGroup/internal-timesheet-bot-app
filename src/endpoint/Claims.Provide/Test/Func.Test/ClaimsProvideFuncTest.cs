using System;
using System.Threading;
using GarageGroup.Infra;
using GarageGroup.Internal.Timesheet.Inner;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.CustomClaims.Test.Func.Test;

public static partial class ClaimsProvideFuncTest
{
    private static readonly ClaimsProvideIn SomeInput 
        = 
        new(new()
        {
            AuthenticationContext = new()
            {
                CorrelationId = Guid.Parse("1282002b-6a8b-418f-b481-67844abb0cc5"),
                User = new()
                {
                    Id = Guid.Parse("5b228f06-d220-4006-844a-374df853108d")
                }
            }
        });

    private static readonly DataverseEntityGetOut<UserJson> SomeGetUserResult
        =
        new(new()
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
