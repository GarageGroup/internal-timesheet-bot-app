using GarageGroup.Infra;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    [Fact]
    public static async Task UpdateAsync_InputProjectTypeIsInvalid_ExpectUnknownFailureCode()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var input = new TimesheetUpdateIn(
            timesheetId: new("b7aee261-3eb4-4d20-8af5-a42c0529a30f"),
            date: new(2021, 11, 07),
            project: new(
                id: new("f7410932-b1ee-47b5-844f-7da94836c433"),
                type: (TimesheetProjectType)7,
                displayName: "Some name"),
            duration: 3,
            description: "Some text");

        var actual = await api.UpdateAsync(input, default);
        var expected = Failure.Create(TimesheetUpdateFailureCode.Unknown, "An unexpected project type: 7");

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.InputUpdateTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task UpdateAsync_InputIsNotNull_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityUpdateIn<TimesheetJson> expectedInput)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var cancellationToken = new CancellationToken(false);
        _ = await api.UpdateAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(a => a.UpdateEntityAsync(expectedInput, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetUpdateFailureCode.NotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetUpdateFailureCode.TooManyRequests)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetUpdateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetUpdateFailureCode.Unknown)]
    public static async Task UpdateTimesheetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetUpdateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseFailure);
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.UpdateAsync(SomeTimesheetUpdateInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task UpdateAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.UpdateAsync(SomeTimesheetUpdateInput, default) ;
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}