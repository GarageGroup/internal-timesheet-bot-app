using System;
using System.Collections.Generic;
using System.Threading;
using DeepEqual.Syntax;
using GarageGroup.Infra;
using Moq;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

public static partial class CrmTimesheetApiTest
{
    private static readonly TimesheetCreateIn SomeTimesheetCreateInput
        =
        new(
            userId: Guid.Parse("56276a44-1444-4f67-bdb7-774b3f25932a"),
            date: new(2021, 10, 07),
            project: new(
                id: Guid.Parse("7583b4e6-23f5-eb11-94ef-00224884a588"),
                type: TimesheetProjectType.Project,
                displayName: "Some project name"),
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

    private static readonly FlatArray<DbTimesheet> SomeDbTimesheetSet
        =
        new DbTimesheet[]
        {
            new() 
            {
                Duration = 8,
                ProjectName = "Some Opportunity",
                Description = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit."
            },
            new()
            {
                Duration = 3,
                ProjectName = null,
                Subject = "Some prject name",
                Description = "Some text"
            }
        };

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

    private static Mock<IDataverseEntityCreateSupplier> BuildMockDataverseCreateSupplier(
        Result<Unit, Failure<DataverseFailureCode>> result)
    {
        var mock = new Mock<IDataverseEntityCreateSupplier>();

        _ = mock
            .Setup(static a => a.CreateEntityAsync(
                It.IsAny<DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static Mock<IDataverseImpersonateSupplier<IDataverseEntityCreateSupplier>> BuildMockDataverseApiClient(
        IDataverseEntityCreateSupplier dataverseCreateSupplier)
    {
        var mock = new Mock<IDataverseImpersonateSupplier<IDataverseEntityCreateSupplier>>();

        _ = mock.Setup(static a => a.Impersonate(It.IsAny<Guid>())).Returns(dataverseCreateSupplier);

        return mock;
    }

    private static Mock<ISqlQueryEntitySetSupplier> BuildMockSqlApi<TDbEntity>(
        Result<FlatArray<TDbEntity>, Failure<Unit>> result)
        where TDbEntity : IDbEntity<TDbEntity>
    {
        var mock = new Mock<ISqlQueryEntitySetSupplier>();

        _ = mock
            .Setup(static a => a.QueryEntitySetOrFailureAsync<TDbEntity>(It.IsAny<IDbQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static bool AreDeepEqual<T>(T expected, T actual)
    {
        actual.ShouldDeepEqual(expected);
        return true;
    }
}