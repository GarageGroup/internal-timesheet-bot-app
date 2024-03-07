using GarageGroup.Infra;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    [Fact]
    public static async Task UpdateAsync_InputIsNull_ExpectArgumentNullException()
    {
        var mockDataverseApiClient = BuildMockUpdateDataverseApiClient(Result.Success<Unit>(default));

        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(TestAsync);

        Assert.Equal("input", ex.ParamName);

        async Task TestAsync()
            =>
            _ = await api.UpdateAsync(null!, default);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.InputUpdateTestData), MemberType = typeof(CrmTimesheetApiSource))]
    public static async Task UpdateAsync_InputIsNotNull_ExpectDataverseUpdateCalledOnce(
        TimesheetUpdateIn input, DataverseEntityUpdateIn<IReadOnlyDictionary<string, object?>> expectedInput)
    {
        var mockDataverseApiClient = BuildMockUpdateDataverseApiClient(Result.Success<Unit>(default));

        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var cancellationToken = new CancellationToken(false);
        _ = await api.UpdateAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(
            a => a.UpdateEntityAsync(It.Is<DataverseEntityUpdateIn<IReadOnlyDictionary<string, object?>>>(
                actual => AreDeepEqual(expectedInput, actual)), cancellationToken),
            Times.Once);
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

        var mockDataverseApiClient = BuildMockUpdateDataverseApiClient(dataverseFailure);

        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.UpdateAsync(SomeTimesheetUpdateInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task UpdateAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApiClient = BuildMockUpdateDataverseApiClient(Result.Success<Unit>(default));

        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.UpdateAsync(SomeTimesheetUpdateInput, default) ;
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}