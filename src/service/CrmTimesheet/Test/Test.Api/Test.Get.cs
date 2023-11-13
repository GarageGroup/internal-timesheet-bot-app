using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiTest
{
    [Fact]
    public static async Task GetAsync_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetItemJson>(SomeTimesheetJsonSetOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var input = new TimesheetSetGetIn(
            userId: Guid.Parse("784b7329-2ad2-ed11-a7c6-6045bd8d750a"),
            date: new(2023, 11, 07));

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(Guid.Parse("784b7329-2ad2-ed11-a7c6-6045bd8d750a")), Times.Once);
    }

    [Fact]
    public static async Task GetAsync_ExpectDataverseApiClientCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetItemJson>(SomeTimesheetJsonSetOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var input = new TimesheetSetGetIn(
            userId: Guid.Parse("bd8b8e33-554e-e611-80dc-c4346bad0190"),
            date: new(2022, 02, 05));

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetAsync(input, cancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_timesheetactivities",
            selectFields: new("gg_date", "gg_duration", "gg_description"),
            expandFields: new(
                new("regardingobjectid_incident", new("title")),
                new("regardingobjectid_lead", new("subject", "companyname")),
                new("regardingobjectid_opportunity", new("name")),
                new("regardingobjectid_gg_project", new("gg_name"))),
            filter: "(_ownerid_value eq 'bd8b8e33-554e-e611-80dc-c4346bad0190' and gg_date eq 2022-02-05)",
            orderBy: new DataverseOrderParameter[]
            {
                new("createdon", DataverseOrderDirection.Ascending)
            });

        mockDataverseApiClient.Verify(a => a.GetEntitySetAsync<TimesheetItemJson>(expectedInput, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Unauthorized, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.Throttling, TimesheetSetGetFailureCode.TooManyRequests)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.RecordNotFound, TimesheetSetGetFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.UserNotEnabled, TimesheetSetGetFailureCode.NotAllowed)]
    [InlineData(DataverseFailureCode.PrivilegeDenied, TimesheetSetGetFailureCode.NotAllowed)]
    public static async Task GetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, TimesheetSetGetFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure message");

        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetItemJson>(dataverseFailure);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var actual = await api.GetAsync(SomeTimesheetSetGetInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.OutputGetTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task GetAsync_DataverseResultIsSuccess_ExpectSuccess(
        DataverseEntitySetGetOut<TimesheetItemJson> dataverseOutput, TimesheetSetGetOut expected)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetItemJson>(dataverseOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var actual = await api.GetAsync(SomeTimesheetSetGetInput, default);
        Assert.StrictEqual(expected, actual);
    }
}