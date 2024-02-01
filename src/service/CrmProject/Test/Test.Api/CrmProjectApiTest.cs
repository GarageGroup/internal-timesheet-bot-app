using System;
using System.Text.Json;
using System.Threading;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

public static partial class CrmProjectApiTest
{
    private static readonly LastProjectSetGetIn SomeLastProjectSetGetInput
        =
        new(
            userId: new("a93ca280-e910-474a-a6a4-e50b5d38ade7"),
            top: 5,
            minDate: new(2023, 07, 25));

    private static readonly ProjectSetSearchIn SomeProjectSetSearchInput
        =
        new(
            searchText: "Some search project name",
            userId: new("45b6e085-4d6e-4b2d-a26c-eb8c1c5a2e5e"),
            top: 10);

    private static readonly FlatArray<DbTimesheetProject> SomeTimesheetProjectSetOutput
        =
        [
            new()
            {
                ProjectId = new("63d9e1b7-706b-ea11-a813-000d3a44ad35"),
                ProjectName = "Some project name",
                ProjectTypeCode = 10912,
                Subject = null
            },
            new()
            {
                ProjectId = new("20f2d7f3-c73d-4895-aa09-c8cdb3cd0acd"),
                ProjectName = "Some lead name",
                ProjectTypeCode = 4,
                Subject = "Some lead subject"
            }
        ];

    private static readonly DataverseSearchOut SomeDataverseSearchOutput
        =
        new(
            totalRecordCount: 3,
            value:
            [
                new(
                    searchScore: 18.698789596557617,
                    objectId: new("cc1efd36-ceca-eb11-bacc-000d3a47050c"),
                    entityName: "opportunity",
                    extensionData: default),
                new(
                    searchScore: 19128,
                    objectId: new("727621b9-e663-44c1-a879-11b53596be4d"),
                    entityName: "lead",
                    extensionData:
                    [
                        new("subject", new(JsonSerializer.SerializeToElement("Some subject"))),
                        new("companyname", new(JsonSerializer.SerializeToElement("Some company name")))
                    ])
            ]);

    private static Mock<ISqlQueryEntitySetSupplier> BuildMockSqlApi(
        in Result<FlatArray<DbTimesheetProject>, Failure<Unit>> result)
    {
        var mock = new Mock<ISqlQueryEntitySetSupplier>();

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbTimesheetProject>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static Mock<IDataverseImpersonateSupplier<IDataverseSearchSupplier>> BuildMockDataverseApiClient(
        IDataverseSearchSupplier dataverseSearchSupplier)
    {
        var mock = new Mock<IDataverseImpersonateSupplier<IDataverseSearchSupplier>>();

        _ = mock.Setup(static a => a.Impersonate(It.IsAny<Guid>())).Returns(dataverseSearchSupplier);

        return mock;
    }

    private static Mock<IDataverseSearchSupplier> BuildMockDataverseSearchSupplier(
        in Result<DataverseSearchOut, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IDataverseSearchSupplier>();

        _ = mock
            .Setup(static a => a.SearchAsync(It.IsAny<DataverseSearchIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }
}