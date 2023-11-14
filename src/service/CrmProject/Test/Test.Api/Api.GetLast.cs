using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiTest
{
    [Fact]
    public static async Task GetLastAsync_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(SomeLastTimesheetSetOutput);
        var todayProvider = BuildTodayProvider(SomeDate);

        var api = new CrmProjectApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, SomeOption);

        var input = new LastProjectSetGetIn(
            userId: Guid.Parse("5f92ef56-a611-4faa-8ef4-92687038d92a"),
            top: 11);

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetLastAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(Guid.Parse("5f92ef56-a611-4faa-8ef4-92687038d92a")), Times.Once);
    }

    [Theory]
    [MemberData(nameof(CrmProjectApiSource.InputGetLastTestData), MemberType = typeof(CrmProjectApiSource))]
    public static async Task GetLastAsync_ExpectDataverseApiClientCalledOnce(
        LastProjectSetGetIn input, CrmProjectApiOption option, DateOnly now, DataverseEntitySetGetIn expectedInput)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(SomeLastTimesheetSetOutput);
        var todayProvider = BuildTodayProvider(now);

        var api = new CrmProjectApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, option);

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetLastAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(a => a.GetEntitySetAsync<LastTimesheetItemJson>(expectedInput, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, ProjectSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, ProjectSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, ProjectSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, ProjectSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, ProjectSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, ProjectSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.Throttling, ProjectSetGetFailureCode.TooManyRequests)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, ProjectSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.DuplicateRecord, ProjectSetGetFailureCode.Unknown)]
    public static async Task GetLastAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, ProjectSetGetFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some Failure message");

        var dataverseResult = Result.Failure(dataverseFailure).With<DataverseEntitySetGetOut<LastTimesheetItemJson>>();
        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseResult);

        var todayProvider = BuildTodayProvider(SomeDate);
        var api = new CrmProjectApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, SomeOption);

        var actual = await api.GetLastAsync(SomeLastProjectSetGetInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmProjectApiSource.OutputGetLastTestData), MemberType = typeof(CrmProjectApiSource))]
    internal static async Task GetLastAsync_DataverseResultIsSuccess_ExpectSuccess(
        LastProjectSetGetIn input, DataverseEntitySetGetOut<LastTimesheetItemJson> dataverseOutput, LastProjectSetGetOut expected)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient(dataverseOutput);
        var todayProvider = BuildTodayProvider(SomeDate);

        var api = new CrmProjectApi<IStubDataverseApi>(mockDataverseApiClient.Object, todayProvider, SomeOption);

        var actual = await api.GetLastAsync(input, default);
        Assert.StrictEqual(expected, actual);
    }
}