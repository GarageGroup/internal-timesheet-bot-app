using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    /*[Fact]
    public static async Task GetLastAsync_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(SomeLastTimesheetSetOutput);
        var todayProvider = BuildTodayProvider(SomeDate);

        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, SomeOption);

        var input = new LastTimesheetSetGetIn(
            userId: Guid.Parse("5f92ef56-a611-4faa-8ef4-92687038d92a"),
            top: 11);

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetLastAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(Guid.Parse("5f92ef56-a611-4faa-8ef4-92687038d92a")), Times.Once);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.InputGetLastTestData), MemberType = typeof(CrmTimesheetApiSource))]
    public static async Task GetLastAsync_CancellationTokenIsNotCanceled_ExpectDataverseApiClientCalledOnce(
        LastTimesheetSetGetIn input, CrmTimesheetApiOption option, DateOnly now, DataverseEntitySetGetIn expectedInput)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(SomeLastTimesheetSetOutput);
        var todayProvider = BuildTodayProvider(now);

        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, option);

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetLastAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(a => a.GetEntitySetAsync<LastTimesheetItemJson>(expectedInput, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetSetGetFailureCode.TooManyRequests)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetSetGetFailureCode.Unknown)]
    public static async Task GetLastAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetSetGetFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some Failure message");

        var dataverseResult = Result.Failure(dataverseFailure).With<DataverseEntitySetGetOut<LastTimesheetItemJson>>();
        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseResult);

        var todayProvider = BuildTodayProvider(SomeDate);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, SomeOption);

        var actual = await api.GetLastAsync(SomeLastTimesheetSetGetInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.OutputGetLastTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task GetLastAsync_DataverseResultIsSuccess_ExpectSuccess(
        LastTimesheetSetGetIn input, DataverseEntitySetGetOut<LastTimesheetItemJson> dataverseOutput, LastTimesheetSetGetOut expected)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseOutput);
        var todayProvider = BuildTodayProvider(SomeDate);

        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, SomeOption);

        var actual = await api.GetLastAsync(input, default);
        Assert.StrictEqual(expected, actual);
    }*/
}