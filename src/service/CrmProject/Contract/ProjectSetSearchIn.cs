using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class ProjectSetSearchIn
{
    public ProjectSetSearchIn([AllowNull] string searchText, Guid userId, int top)
    {
        SearchText = searchText.OrEmpty();
        UserId = userId;
        Top = top;
    }

    public string SearchText { get; }

    public Guid UserId { get; }

    public int Top { get; }
}
