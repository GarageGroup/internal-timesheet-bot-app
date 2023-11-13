using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using GarageGroup.Infra;
using Moq;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

public static partial class CrmProjectApiTest
{
    private static readonly LastProjectSetGetIn SomeLastProjectSetGetInput
        =
        new(
            userId: Guid.Parse("a93ca280-e910-474a-a6a4-e50b5d38ade7"),
            top: 5);

    private static readonly ProjectSetSearchIn SomeProjectSetSearchInput
        =
        new(
            searchText: "Some search project name",
            userId: Guid.Parse("45b6e085-4d6e-4b2d-a26c-eb8c1c5a2e5e"),
            top: 10);

    private static readonly CrmProjectApiOption SomeOption
        =
        new()
        {
            LastProjectDaysCount = 30,
            LastProjectItemsCount = 90
        };

    private static readonly DateOnly SomeDate
        =
        new(2021, 11, 21);

    private static readonly DataverseEntitySetGetOut<LastTimesheetItemJson> SomeLastTimesheetSetOutput
        =
        new(
            value: new LastTimesheetItemJson[]
            {
                new()
                {
                    Project = new()
                    {
                        Id = Guid.Parse("63d9e1b7-706b-ea11-a813-000d3a44ad35"),
                        Name = "Some project name"
                    },
                    TimesheetDate = new(2022, 01, 15)
                },
                new()
                {
                    Lead = new()
                    {
                        Id = Guid.Parse("20f2d7f3-c73d-4895-aa09-c8cdb3cd0acd"),
                        Subject = "Some lead subject",
                        CompanyName = "Some lead company name"
                    },
                    TimesheetDate = new(2023, 11, 03)
                }
            });

    private static readonly DataverseSearchOut SomeDataverseSearchOutput
        =
        new(
            totalRecordCount: 3,
            value: new DataverseSearchItem[]
            {
                new(
                    searchScore: 18.698789596557617,
                    objectId: Guid.Parse("cc1efd36-ceca-eb11-bacc-000d3a47050c"),
                    entityName: "opportunity",
                    extensionData: default),
                new(
                    searchScore: 19128,
                    objectId: Guid.Parse("727621b9-e663-44c1-a879-11b53596be4d"),
                    entityName: "lead",
                    extensionData: new KeyValuePair<string, DataverseSearchJsonValue>[]
                    {
                        new("subject", new(JsonSerializer.SerializeToElement("Some subject"))),
                        new("companyname", new(JsonSerializer.SerializeToElement("Some company name")))
                    })
            });

    private static Mock<IStubDataverseApi> BuildMockDataverseApiClient(
        Result<DataverseEntitySetGetOut<LastTimesheetItemJson>, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IStubDataverseApi>();

        _ = mock
            .Setup(static a => a.GetEntitySetAsync<LastTimesheetItemJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _ = mock.Setup(static a => a.Impersonate(It.IsAny<Guid>())).Returns(mock.Object);

        return mock;
    }

    private static Mock<IStubDataverseApi> BuildMockDataverseApiClient(
        Result<DataverseSearchOut, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IStubDataverseApi>();

        _ = mock
            .Setup(static a => a.SearchAsync(It.IsAny<DataverseSearchIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _ = mock.Setup(static a => a.Impersonate(It.IsAny<Guid>())).Returns(mock.Object);

        return mock;
    }

    private static ITodayProvider BuildTodayProvider(DateOnly now)
        =>
        Mock.Of<ITodayProvider>(p => p.GetToday() == now);

    internal interface IStubDataverseApi : IDataverseEntitySetGetSupplier, IDataverseSearchSupplier, IDataverseImpersonateSupplier<IStubDataverseApi>
    {
    }
}