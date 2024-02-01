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
    public static async Task GetAsync_ExpectSqlApiCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi<DbTimesheet>(SomeDbTimesheetSet);
        var api = new CrmTimesheetApi(Mock.Of<IDataverseImpersonateSupplier<IDataverseEntityCreateSupplier>>(), mockSqlApi.Object, SomeOption);

        var input = new TimesheetSetGetIn(
            userId: new("bd8b8e33-554e-e611-80dc-c4346bad0190"),
            date: new(2022, 02, 05));

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetAsync(input, cancellationToken);

        var expectedQuery = new DbSelectQuery("gg_timesheetactivity", "t")
        {
            SelectedFields =
            [
                "t.gg_duration AS Duration",
                "t.regardingobjectidname AS ProjectName",
                "t.subject AS Subject",
                "t.gg_description AS Description"
            ],
            Filter = new DbCombinedFilter(DbLogicalOperator.And)
            {
                Filters =
                [
                    new DbParameterFilter("t.ownerid", DbFilterOperator.Equal, Guid.Parse("bd8b8e33-554e-e611-80dc-c4346bad0190"), "ownerId"),
                    new DbParameterFilter("t.gg_date", DbFilterOperator.Equal, "2022-02-05", "date")
                ]
            },
            Orders =
            [
                new("t.createdon", DbOrderType.Ascending)
            ]
        };

        mockSqlApi.Verify(a => a.QueryEntitySetOrFailureAsync<DbTimesheet>(expectedQuery, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task GetAsync_DbResultIsFailure_ExpectUnknownFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some failure message");

        var mockSqlApi = BuildMockSqlApi<DbTimesheet>(dbFailure);
        var api = new CrmTimesheetApi(Mock.Of<IDataverseImpersonateSupplier<IDataverseEntityCreateSupplier>>(), mockSqlApi.Object, SomeOption);

        var actual = await api.GetAsync(SomeTimesheetSetGetInput, default);
        var expected = Failure.Create(TimesheetSetGetFailureCode.Unknown, "Some failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.OutputGetTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task GetAsync_DataverseResultIsSuccess_ExpectSuccess(
        FlatArray<DbTimesheet> dbTimesheets, TimesheetSetGetOut expected)
    {
        var mockSqlApi = BuildMockSqlApi<DbTimesheet>(dbTimesheets);
        var api = new CrmTimesheetApi(Mock.Of<IDataverseImpersonateSupplier<IDataverseEntityCreateSupplier>>(), mockSqlApi.Object, SomeOption);

        var actual = await api.GetAsync(SomeTimesheetSetGetInput, default);
        Assert.StrictEqual(expected, actual);
    }
}