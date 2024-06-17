using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using GarageGroup.Internal.Timesheet.Inner;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CustomClaims.Test.Func.Test;

partial class ProvideClaimsFuncTest
{
    [Fact]
    public static async Task InvokeAsync_ExpectGetUserCalledOnce()
    {
        var cancellationToken = CancellationToken.None;
        var input = new ProvideClaimsIn(new AuthenticationEventData
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
        var func = new ProvideClaimsFunc(mockDataverseApi.Object);
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
    
    [Fact]
    public static async Task InvokeAsync_ExpectValidationError()
    {
        var input = new ProvideClaimsIn(new AuthenticationEventData());
        var cancellationToken = CancellationToken.None;

        var mockDataverseApi = BuildDataverseMock(SomeGetUserResult);
        var func = new ProvideClaimsFunc(mockDataverseApi.Object);
        
        var actual = await func.InvokeAsync(input, cancellationToken);
        var expected = Failure.Create(ProvideClaimsFailureCode.InvalidQuery, "The Azure Active Directory user is not specified");
        
        Assert.StrictEqual(actual, expected);
    }
    
    [Theory]
    [InlineData(DataverseFailureCode.Unknown, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, ProvideClaimsFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidFileSize, ProvideClaimsFailureCode.Unknown)]
    public static async Task InvokeAsync_GetUserResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, ProvideClaimsFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");
        
        var mockDataverseApi = BuildDataverseMock(dataverseFailure);
        var func = new ProvideClaimsFunc(mockDataverseApi.Object);

        var cancellationToken = CancellationToken.None;
        
        var actual = await func.InvokeAsync(SomeInput, cancellationToken);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);
        
        Assert.StrictEqual(expected, actual);
    }
}