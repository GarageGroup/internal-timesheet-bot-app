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
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.InvalidPayload)]
    [InlineData(DataverseFailureCode.InvalidFileSize)]
    public static async Task InvokeAsync_GetUserResultIsFailure_ExpectUnknownFailure(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");
        
        var mockDataverseApi = BuildDataverseMock(dataverseFailure);
        var func = new ClaimsProvideFunc(mockDataverseApi.Object);

        var cancellationToken = CancellationToken.None;
        
        var actual = await func.InvokeAsync(SomeInput, cancellationToken);
        var expected = Failure.Create(ClaimsProvideFailureCode.Unknown, "Some failure text", sourceException);
        
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

        var func = new ClaimsProvideFunc(mockDataverseApi.Object);
        var actual = await func.InvokeAsync(SomeInput, CancellationToken.None);

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
                            SystemUserId = Guid.Parse("eea93cde-6bbf-4137-a6b9-f6c75d8ea10c")
                        }
                    }
                ]
            }
        };
        
        Assert.StrictEqual(actual, expected);
    }
}
