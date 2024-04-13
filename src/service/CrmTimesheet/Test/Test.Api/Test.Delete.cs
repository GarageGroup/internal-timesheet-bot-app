using GarageGroup.Infra;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.InputDeleteTestData), MemberType = typeof(CrmTimesheetApiSource))]
    public static async Task DeleteAsync_ExpectDataverseDeleteCalledOnce(
        TimesheetDeleteIn input, DataverseEntityDeleteIn expectedInput)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var cancellationToken = new CancellationToken(false);
        _ = await api.DeleteAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(a => a.DeleteEntityAsync(expectedInput, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetDeleteFailureCode.NotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetDeleteFailureCode.TooManyRequests)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetDeleteFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetDeleteFailureCode.Unknown)]
    public static async Task DeleteTimesheetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetDeleteFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseFailure);
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.DeleteAsync(SomeTimesheetDeleteInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task DeleteAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.DeleteAsync(SomeTimesheetDeleteInput, default) ;
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}