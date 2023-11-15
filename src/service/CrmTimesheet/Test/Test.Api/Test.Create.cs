using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    [Fact]
    public static async Task CreateAsync_InputIsNull_ExpectArgumentNullException()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));

        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(TestAsync);

        Assert.Equal("input", ex.ParamName);

        async Task TestAsync()
            =>
            _ = await api.CreateAsync(null!, default);
    }

    [Fact]
    public static async Task CreateAsync_InputIsNotNull_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var input = new TimesheetCreateIn(
            userId: Guid.Parse("4698fc58-770b-4a53-adcd-592eaded6f87"),
            date: new(2023, 05, 21),
            projectId: Guid.Parse("5cef9828-c94b-4ca0-bab5-28c1a45d95ef"),
            projectType: TimesheetProjectType.Lead,
            duration: 3,
            description: "Some description text",
            channel: TimesheetChannel.Teams);

        var cancellationToken = new CancellationToken(false);
        _ = await api.CreateAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(Guid.Parse("4698fc58-770b-4a53-adcd-592eaded6f87")), Times.Once);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.InputCreateTestData), MemberType = typeof(CrmTimesheetApiSource))]
    public static async Task CreateAsync_InputIsNotNull_ExpectDataverseApiCalledOnce(
        TimesheetCreateIn input, CrmTimesheetApiOption option, DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>> expectedInput)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), option);

        var cancellationToken = new CancellationToken(false);
        _ = await api.CreateAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(
            a => a.CreateEntityAsync(It.Is<DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>>(
                actual => AreDeepEqual(expectedInput, actual)), cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetCreateFailureCode.NotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetCreateFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetCreateFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetCreateFailureCode.TooManyRequests)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetCreateFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetCreateFailureCode.Unknown)]
    public static async Task CreateTimesheetSetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetCreateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var dataverseResult = Result.Failure(dataverseFailure).With<Unit>();
        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseResult);

        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.CreateAsync(SomeTimesheetCreateInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task CreateAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>(), SomeOption);

        var actual = await api.CreateAsync(SomeTimesheetCreateInput, default);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}