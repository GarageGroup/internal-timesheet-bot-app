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
    public static async Task SearchAsync_InputIsNull_ExpectArgumentNullException()
    {
        var mockDataverseSearchSupplier = BuildMockDataverseSearchSupplier(SomeDataverseSearchOutput);
        var mockDataverseApiClient = BuildMockDataverseApiClient(mockDataverseSearchSupplier.Object);

        var api = new CrmProjectApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(TestAsync);

        Assert.Equal("input", ex.ParamName);

        async Task TestAsync()
            =>
            _ = await api.SearchAsync(null!, default);
    }

    [Fact]
    public static async Task SearchAsync_InputIsNotNull_ExpectDataverseImpersonateCalledOnce()
    {
        var mockDataverseSearchSupplier = BuildMockDataverseSearchSupplier(SomeDataverseSearchOutput);
        var mockDataverseApiClient = BuildMockDataverseApiClient(mockDataverseSearchSupplier.Object);

        var api = new CrmProjectApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var input = new ProjectSetSearchIn(
            searchText: "Some search text",
            userId: Guid.Parse("3589bc60-227f-4aa6-a5c3-4248304a1b49"),
            top: 3);

        var cancellationToken = new CancellationToken(false);
        _ = await api.SearchAsync(input, cancellationToken);

        mockDataverseApiClient.Verify(static a => a.Impersonate(Guid.Parse("3589bc60-227f-4aa6-a5c3-4248304a1b49")), Times.Once);
    }

    [Theory]
    [MemberData(nameof(CrmProjectApiSource.InputSearchTestData), MemberType = typeof(CrmProjectApiSource))]
    public static async Task SearchAsync_InputIsNotNull_ExpectDataverseSearchCalledOnce(
        ProjectSetSearchIn input, DataverseSearchIn expectedInput)
    {
        var mockDataverseSearchSupplier = BuildMockDataverseSearchSupplier(SomeDataverseSearchOutput);
        var mockDataverseApiClient = BuildMockDataverseApiClient(mockDataverseSearchSupplier.Object);

        var api = new CrmProjectApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var cancellationToken = new CancellationToken(false);
        _ = await api.SearchAsync(input, cancellationToken);

        mockDataverseSearchSupplier.Verify(a => a.SearchAsync(expectedInput, cancellationToken), Times.Once);
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
    public static async Task SearchProjectSetAsync_DataverseResultIsFailure_ExpectFailure(
        DataverseFailureCode sourceFailureCode, ProjectSetGetFailureCode expectedFailureCode)
    {
        var sourceException = new Exception("Some exception message");
        var dataverseFailure = sourceException.ToFailure(sourceFailureCode, "Some failure text");

        var mockDataverseSearchSupplier = BuildMockDataverseSearchSupplier(dataverseFailure);
        var mockDataverseApiClient = BuildMockDataverseApiClient(mockDataverseSearchSupplier.Object);

        var api = new CrmProjectApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());

        var actual = await api.SearchAsync(SomeProjectSetSearchInput, default);
        var expected = Failure.Create(expectedFailureCode, "Some failure text", sourceException);

        Assert.StrictEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CrmProjectApiSource.OutputSearchTestData), MemberType = typeof(CrmProjectApiSource))]
    public static async Task SearchAsync_DataverseResultIsSuccess_ExpectSuccess(
        DataverseSearchOut dataverseOutput, ProjectSetSearchOut expected)
    {
        var mockDataverseSearchSupplier = BuildMockDataverseSearchSupplier(dataverseOutput);
        var mockDataverseApiClient = BuildMockDataverseApiClient(mockDataverseSearchSupplier.Object);

        var api = new CrmProjectApi(mockDataverseApiClient.Object, Mock.Of<ISqlQueryEntitySetSupplier>());
        var actual = await api.SearchAsync(SomeProjectSetSearchInput, default);

        Assert.StrictEqual(expected, actual);
    }
}