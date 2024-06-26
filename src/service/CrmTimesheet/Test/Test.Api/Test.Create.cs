﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    [Fact]
    public static async Task CreateAsync_InputProjectTypeIsInvalid_ExpectUnknownFailureCode()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var input = new TimesheetCreateIn(
            userId: new("00889262-2cd5-4084-817b-d810626f2600"),
            date: new(2021, 11, 07),
            project: new(
                id: new("f7410932-b1ee-47b5-844f-7da94836c433"),
                type: (TimesheetProjectType)1,
                displayName: "Some name"),
            duration: 3,
            description: "Some text");

        var actual = await api.CreateAsync(input, default);
        var expected = Failure.Create(TimesheetCreateFailureCode.Unknown, "An unexpected project type: 1");

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task CreateAsync_InputIsValid_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var input = new TimesheetCreateIn(
            userId: new("4698fc58-770b-4a53-adcd-592eaded6f87"),
            date: new(2023, 05, 21),
            project: new(
                id: new("5cef9828-c94b-4ca0-bab5-28c1a45d95ef"),
                type: TimesheetProjectType.Lead,
                displayName: default),
            duration: 3,
            description: "Some description text");

        var cancellationToken = new CancellationToken(false);
        _ = await api.CreateAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(new("4698fc58-770b-4a53-adcd-592eaded6f87")), Times.Once);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.InputCreateTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task CreateAsync_InputIsValid_ExpectDataverseCreateCalledOnce(
        TimesheetCreateIn input, DataverseEntityCreateIn<TimesheetJson> expectedInput)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var cancellationToken = new CancellationToken(false);
        _ = await api.CreateAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(a => a.CreateEntityAsync(expectedInput, cancellationToken), Times.Once);
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
    [InlineData(DataverseFailureCode.InvalidPayload, TimesheetCreateFailureCode.Unknown)]
    public static async Task CreateTimesheetSetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetCreateFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseFailure);
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var actual = await api.CreateAsync(SomeTimesheetCreateInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Fact]
    public static async Task CreateAsync_DataverseResultIsSuccess_ExpectSuccess()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(Result.Success<Unit>(default));
        var api = new CrmTimesheetApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var actual = await api.CreateAsync(SomeTimesheetCreateInput, default);
        var expected = Result.Success<Unit>(default);

        Assert.StrictEqual(expected, actual);
    }
}