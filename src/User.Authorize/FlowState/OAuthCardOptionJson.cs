using System;

namespace GGroupp.Internal.Timesheet;

internal readonly record struct OAuthCardOptionJson
{
    public string? Title { get; init; }

    public string? Text { get; init; }

    public string? ConnectionName { get; init; }

    public TimeSpan? Timeout { get; init; }
}