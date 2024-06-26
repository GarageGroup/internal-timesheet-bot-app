using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using GarageGroup.Internal.Timesheet.Inner;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CustomClaims.Test.Func.Test;

partial class ClaimsProvideFuncTest
{
    [Fact]
    public static async Task InvokeAsync_InputIsInvalid_ExpectValidationFailure()
    {
        var input = new ClaimsProvideIn(new AuthenticationEventData());
        var cancellationToken = CancellationToken.None;

        var mockDataverseApi = BuildDataverseMock(SomeGetUserResult);
        var func = new ClaimsProvideFunc(mockDataverseApi.Object);
        
        var actual = await func.InvokeAsync(input, cancellationToken);
        var expected = Failure.Create(ClaimsProvideFailureCode.InvalidQuery, "The Azure Active Directory user is not specified");
        
        Assert.StrictEqual(actual, expected);
    }

    [Fact]
    public static async Task InvokeAsync_InputIsValid_ExpectGetUserCalledOnce()
    {
        var cancellationToken = CancellationToken.None;
        var input = new ClaimsProvideIn(new AuthenticationEventData
        {
            AuthenticationContext = new AuthenticationContext
            {
                User = new User
                {
                    Id = Guid.Parse("7b3250c2-1f63-4125-bc6c-c35dfb70b9ea")
                }
            }
        });
        
        var mockDataverseApi = BuildDataverseMock(SomeGetUserResult);
        var func = new ClaimsProvideFunc(mockDataverseApi.Object);
        _ = await func.InvokeAsync(input, cancellationToken);
        
        var expected = new DataverseEntityGetIn(
            entityPluralName: "systemusers",
            selectFields: ["systemuserid"],
            entityKey: new DataverseAlternateKey(
                fieldName: "azureactivedirectoryobjectid",
                fieldValue: "7b3250c2-1f63-4125-bc6c-c35dfb70b9ea"));
        
        mockDataverseApi.Verify(
            x => x.GetEntityAsync<UserJson>(expected, cancellationToken),
            Times.Once);
    }
    
    [Theory]
    [InlineData(DataverseFailureCode.Unknown, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, ClaimsProvideFailureCode.UserNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, ClaimsProvideFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, ClaimsProvideFailureCode.Unknown)]
    public static async Task InvokeAsync_GetUserResultIsFailure_ExpectUnknownFailure(
        DataverseFailureCode sourceFailureCode, ClaimsProvideFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");
        
        var mockDataverseApi = BuildDataverseMock(dataverseFailure);
        var func = new ClaimsProvideFunc(mockDataverseApi.Object);

        var cancellationToken = CancellationToken.None;
        
        var actual = await func.InvokeAsync(SomeInput, cancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);
        
        Assert.StrictEqual(expected, actual);
    }
    
    [Fact]
    public static async Task InvokeAsync_GetUserResultIsSuccess_ExpectSuccess()
    {
        var dataverseGetUserResult = new DataverseEntityGetOut<UserJson>(new UserJson
        {
            Id = Guid.Parse("eea93cde-6bbf-4137-a6b9-f6c75d8ea10c")
        });
        var mockDataverseApi = BuildDataverseMock(dataverseGetUserResult);

        var input = new ClaimsProvideIn(
            new AuthenticationEventData
            {
                AuthenticationContext = new AuthenticationContext
                {
                    CorrelationId = Guid.Parse("97d7f13b-24e2-461e-9b57-8d8e3d9d9ba3"),
                    User = new User
                    {
                        Id = Guid.Parse("ac9fdb67-8321-4757-8bd8-3066ca55f7d9")
                    }
                }
            });
        
        var func = new ClaimsProvideFunc(mockDataverseApi.Object);
        var actual = await func.InvokeAsync(input, CancellationToken.None);

        var expected = new ClaimsProvideOut
        {
            Data = new AuthenticationEventResponseData
            {
                Actions =
                [
                    new TokenIssuanceAction
                    {
                        Claims = new Claims
                        {
                            CorrelationId = Guid.Parse("97d7f13b-24e2-461e-9b57-8d8e3d9d9ba3"),
                            SystemUserId = Guid.Parse("eea93cde-6bbf-4137-a6b9-f6c75d8ea10c")
                        }
                    }
                ]
            }
        };
        
        Assert.StrictEqual(actual, expected);
    }
}
