using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record ProjectCacheJson
{
    public Guid Id { get; init; }

    public string? Name { get; init; }

    public string? Data { get; init; }
}