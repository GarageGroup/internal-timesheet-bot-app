using System.Diagnostics.CodeAnalysis;

namespace GGroupp.Internal.Timesheet;

internal readonly record struct FlowFailure
{
    private readonly string? userMessage;

    public FlowFailure([AllowNull] string? userMessage)
        =>
        this.userMessage = string.IsNullOrEmpty(userMessage) ? default : userMessage;

    public string UserMessage => userMessage ?? string.Empty;
}