using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Moq;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiTest
{
    [Theory]
    [MemberData(nameof(CrmProjectApiSource.InputGetLastTestData), MemberType = typeof(CrmProjectApiSource))]
    public static async Task GetLastAsync_ExpectMockSqlApiCalledOnce(
        LastProjectSetGetIn input, DbSelectQuery expectedQuery)
    {
        var mockSqlApi = BuildMockSqlApi(SomeTimesheetProjectSetOutput);
        var api = new CrmProjectApi<IStubDataverseApi>(Mock.Of<IStubDataverseApi>(), mockSqlApi.Object);

        var cancellationToken = new CancellationToken(false);
        _ = await api.GetLastAsync(input, cancellationToken);

        mockSqlApi.Verify(a => a.QueryEntitySetOrFailureAsync<DbTimesheetProject>(expectedQuery, cancellationToken), Times.Once);
    }

    [Fact]
    public static async Task GetLastAsync_DbResultIsFailure_ExpectUnknownFailure()
    {
        var sourceException = new Exception("Some error message");
        var dbFailure = sourceException.ToFailure("Some Failure message");

        var mockSqlApi = BuildMockSqlApi(dbFailure);
        var api = new CrmProjectApi<IStubDataverseApi>(Mock.Of<IStubDataverseApi>(), mockSqlApi.Object);

        var actual = await api.GetLastAsync(SomeLastProjectSetGetInput, default);
        var expected = Failure.Create(ProjectSetGetFailureCode.Unknown, "Some Failure message", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmProjectApiSource.OutputGetLastTestData), MemberType = typeof(CrmProjectApiSource))]
    internal static async Task GetLastAsync_DataverseResultIsSuccess_ExpectSuccess(
        FlatArray<DbTimesheetProject> dbTimesheetProjects, LastProjectSetGetOut expected)
    {
        var mockSqlApi = BuildMockSqlApi(dbTimesheetProjects);
        var api = new CrmProjectApi<IStubDataverseApi>(Mock.Of<IStubDataverseApi>(), mockSqlApi.Object);

        var actual = await api.GetLastAsync(SomeLastProjectSetGetInput, default);
        Assert.StrictEqual(expected, actual);
    }
}