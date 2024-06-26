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
    public static async Task GetTagSetAsync_ExpectSqlApiCalledOnce()
    {
        var mockSqlApi = BuildMockSqlApi<DbTimesheetTag>(SomeDbTimesheetTagSet);
        var api = new CrmTimesheetApi(Mock.Of<IDataverseApiClient>(), mockSqlApi.Object);

        var input = new TimesheetTagSetGetIn(
            userId: new("82ee3d26-17f1-4e2f-adb2-eeea5119a512"),
            projectId: new("58482d23-ca3e-4499-8294-cc9b588cce73"),
            minDate: new(2023, 06, 15),
            maxDate: new(2023, 11, 03));

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetTagSetAsync(input, cancellationToken);

        var expectedQuery = new DbSelectQuery("gg_timesheetactivity", "t")
        {
            SelectedFields = new("t.gg_description AS Description"),
            Filter = new DbCombinedFilter(DbLogicalOperator.And)
            {
                Filters =
                [
                    new DbParameterFilter(
                        "t.ownerid", DbFilterOperator.Equal, Guid.Parse("82ee3d26-17f1-4e2f-adb2-eeea5119a512"), "ownerId"),
                    new DbParameterFilter(
                        "t.regardingobjectid", DbFilterOperator.Equal, Guid.Parse("58482d23-ca3e-4499-8294-cc9b588cce73"), "projectId"),
                    new DbLikeFilter(
                        "t.gg_description", "%#%", "description"),
                    new DbParameterFilter(
                        "t.gg_date", DbFilterOperator.GreaterOrEqual, "2023-06-15", "minDate"),
                    new DbParameterFilter(
                        "t.gg_date", DbFilterOperator.LessOrEqual, "2023-11-03", "maxDate")
                ]
            },
            Orders =
            [
                new("t.gg_date", DbOrderType.Descending),
                new("t.createdon", DbOrderType.Descending)
            ]
        };

        mockSqlApi.Verify(a => a.QueryEntitySetOrFailureAsync<DbTimesheetTag>(expectedQuery, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task GetTagSetAsync_DbResultIsFailure_ExpectFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some failure text");

        var mockSqlApi = BuildMockSqlApi<DbTimesheetTag>(dbFailure);
        var api = new CrmTimesheetApi(Mock.Of<IDataverseApiClient>(), mockSqlApi.Object);

        var actual = await api.GetTagSetAsync(SomeTimesheetTagSetGetInput, default);
        var expected = Failure.Create("Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmTimesheetApiSource.OutputGetTagSetTestData), MemberType = typeof(CrmTimesheetApiSource))]
    internal static async Task GetTagSetAsync_DbResultIsSuccess_ExpectSuccess(
        FlatArray<DbTimesheetTag> dbTimesheetTags, TimesheetTagSetGetOut expected)
    {
        var mockSqlApi = BuildMockSqlApi<DbTimesheetTag>(dbTimesheetTags);
        var api = new CrmTimesheetApi(Mock.Of<IDataverseApiClient>(), mockSqlApi.Object);

        var actual = await api.GetTagSetAsync(SomeTimesheetTagSetGetInput, default);
        Assert.StrictEqual(expected, actual);
    }
}