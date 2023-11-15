using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DeepEqual.Syntax;
using GarageGroup.Infra;
using Moq;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

public static partial class CrmTimesheetApiTest
{
    private static readonly TimesheetCreateIn SomeTimesheetCreateInput
        =
        new(
            userId: Guid.Parse("56276a44-1444-4f67-bdb7-774b3f25932a"),
            date: new(2021, 10, 07),
            projectId: Guid.Parse("7583b4e6-23f5-eb11-94ef-00224884a588"),
            projectType: TimesheetProjectType.Project,
            duration: 9,
            description: "Some description",
            channel: TimesheetChannel.Telegram);

    private static readonly TimesheetSetGetIn SomeTimesheetSetGetInput
        =
        new(
            userId: Guid.Parse("bd8b8e33-554e-e611-80dc-c4346bad0190"),
            date: new(2022, 02, 07));

    private static readonly TimesheetTagSetGetIn SomeTimesheetTagSetGetInput
        =
        new(
            userId: Guid.Parse("54f0d2cf-93a3-417e-a21a-bff4e16c1b25"),
            projectId: Guid.Parse("6f8f07d6-b7e4-4b00-a829-e680c0375d1e"),
            minDate: new(2023, 07, 24),
            maxDate: new(2023, 08, 01));

    private static readonly CrmTimesheetApiOption SomeOption
        =
        new(
            channelCodes: new KeyValuePair<TimesheetChannel, int?>[]
            {
                new(TimesheetChannel.Teams, 167001),
                new(TimesheetChannel.Telegram, 167002)
            });

    private static readonly DataverseEntitySetGetOut<TimesheetItemJson> SomeTimesheetJsonSetOutput
        =
        new(
            value: new TimesheetItemJson[]
            {
                new() 
                {
                    Date = new(2022, 02, 07, 01, 01, 01, default),
                    Duration = 8,
                    Opportunity = new()
                    {
                        Id = Guid.Parse("ba0d9b46-9a09-4196-8b1a-d69e1a28d7d2"),
                        Name = "Some Opportunity"
                    },
                    Description = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit."
                },
                new()
                {
                    Date = new(2022, 02, 07, 01, 01, 01, default),
                    Duration = 8,
                    Project = new()
                    {
                        Id = Guid.Parse("da7c71e9-1f8a-451c-83a6-997dac339d72"),
                        Name = "Some prject name"
                    },
                    Description = "Some text"
                }
            });

    private static readonly FlatArray<DbTimesheetTag> SomeDbTimesheetTagSet
        =
        new DbTimesheetTag[]
        {
            new() 
            {
                Description = "#TaskOne. Some text"
            },
            new()
            {
                Description = "#TaskTwo. More text"
            }
        };

    private static Mock<IStubDataverseApi> BuildMockDataverseApiClient(
        Result<Unit, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IStubDataverseApi>();

        _ = mock
            .Setup(static a => a.CreateEntityAsync(
                It.IsAny<DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _ = mock.Setup(static a => a.Impersonate(It.IsAny<Guid>())).Returns(mock.Object);

        return mock;
    }

    private static Mock<IStubDataverseApi> BuildMockDataverseApiClient<TEntityJson>(
        Result<DataverseEntitySetGetOut<TEntityJson>, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IStubDataverseApi>();

        _ = mock
            .Setup(static a => a.GetEntitySetAsync<TEntityJson>(It.IsAny<DataverseEntitySetGetIn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _ = mock.Setup(static a => a.Impersonate(It.IsAny<Guid>())).Returns(mock.Object);

        return mock;
    }

    private static Mock<ISqlQueryEntitySetSupplier> BuildMockSqlApi(
        Result<FlatArray<DbTimesheetTag>, Failure<Unit>> result)
    {
        var mock = new Mock<ISqlQueryEntitySetSupplier>();

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<DbTimesheetTag>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static bool AreDeepEqual<T>(T expected, T actual)
    {
        actual.ShouldDeepEqual(expected);
        return true;
    }

    internal interface IStubDataverseApi
        : IDataverseEntitySetGetSupplier, IDataverseEntityCreateSupplier, IDataverseImpersonateSupplier<IStubDataverseApi>
    {
    }
}