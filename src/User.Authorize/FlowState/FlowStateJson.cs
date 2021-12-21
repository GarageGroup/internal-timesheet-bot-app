using System;

namespace GGroupp.Internal.Timesheet;

internal readonly record struct FlowStateJson
{
    public DateTimeOffset ExpirationDate { get; init; }

    public OAuthCardOptionJson? Option { get; init; }

    public CallerInfoJson? CallerInfo { get; init; }
}