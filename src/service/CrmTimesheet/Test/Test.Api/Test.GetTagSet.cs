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
    public static async Task GetTagSetAsync_InputIsNull_ExpectArgumentNullException()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetTagJson>(SomeTimesheetTagJsonSetOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(TestAsync);
        Assert.Equal("input", ex.ParamName);

        async Task TestAsync()
            =>
            _ = await api.GetTagSetAsync(null!, default);
    }

    [Fact]
    public static async Task GetTagSetAsync_InputIsNotNull_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetTagJson>(SomeTimesheetTagJsonSetOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var input = new TimesheetTagSetGetIn(
            userId: Guid.Parse("35e89c69-a7b4-4513-87ee-8c353cf84fc9"),
            projectId: Guid.Parse("80c9f40b-5f28-4ca7-b6ec-91eda71fe12b"),
            minDate: new(2023, 01, 27));

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetTagSetAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(Guid.Parse("35e89c69-a7b4-4513-87ee-8c353cf84fc9")), Times.Once);
    }

    [Fact]
    public static async Task GetTagSetAsync_InputIsNotNull_ExpectDataverseApiClientCalledOnce()
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetTagJson>(SomeTimesheetTagJsonSetOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var input = new TimesheetTagSetGetIn(
            userId: Guid.Parse("82ee3d26-17f1-4e2f-adb2-eeea5119a512"),
            projectId: Guid.Parse("58482d23-ca3e-4499-8294-cc9b588cce73"),
            minDate: new(2023, 06, 15));

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetTagSetAsync(input, cancellationToken);

        var expectedInput = new DataverseEntitySetGetIn(
            entityPluralName: "gg_timesheetactivities",
            selectFields: new("gg_description"),
            filter:
                "(_ownerid_value eq '82ee3d26-17f1-4e2f-adb2-eeea5119a512' " +
                "and _regardingobjectid_value eq '58482d23-ca3e-4499-8294-cc9b588cce73' " +
                "and contains(gg_description, '%23') " +
                "and gg_date ge 2023-06-15)",
            orderBy: new DataverseOrderParameter[]
            {
                new("createdon", DataverseOrderDirection.Descending)
            });

        mockDataverseApiClient.Verify(a => a.GetEntitySetAsync<TimesheetTagJson>(expectedInput, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(DataverseFailureCode.Unknown)]
    [InlineData(DataverseFailureCode.DuplicateRecord)]
    [InlineData(DataverseFailureCode.Unauthorized)]
    [InlineData(DataverseFailureCode.Throttling)]
    [InlineData(DataverseFailureCode.SearchableEntityNotFound)]
    [InlineData(DataverseFailureCode.PicklistValueOutOfRange)]
    [InlineData(DataverseFailureCode.RecordNotFound)]
    [InlineData(DataverseFailureCode.UserNotEnabled)]
    [InlineData(DataverseFailureCode.PrivilegeDenied)]
    public static async Task GetTagSetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode)
    {
        var sourceException = new Exception("Some error message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetTagJson>(dataverseFailure);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var actual = await api.GetTagSetAsync(SomeTimesheetTagSetGetInput, default);
        var expected = Failure.Create("Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.OutputGetTagSetTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task GetTagSetAsync_DataverseResultIsSuccess_ExpectSuccess(
        DataverseEntitySetGetOut<TimesheetTagJson> dataverseOutput, TimesheetTagSetGetOut expected)
    {
        var mockDataverseApiClient = BuildMockDataverseApiClient<TimesheetTagJson>(dataverseOutput);
        var api = new CrmTimesheetApi<IStubDataverseApi>(mockDataverseApiClient.Object, SomeOption);

        var actual = await api.GetTagSetAsync(SomeTimesheetTagSetGetInput, default);
        Assert.StrictEqual(expected, actual);
    }
}