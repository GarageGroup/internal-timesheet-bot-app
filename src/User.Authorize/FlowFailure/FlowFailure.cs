using System.Diagnostics.CodeAnalysis;

namespace GGroupp.Internal.Timesheet;

internal readonly record struct FlowFailure
{
    public const string UnexpectedFailureMessage
        =
        "Возникла непредвиденная ошибка при попытке получить данные пользователя. Повторите попытку позже или обратитесь к администратору";

    private readonly string? userMessage;

    public FlowFailure([AllowNull] string? userMessage)
        =>
        this.userMessage = string.IsNullOrEmpty(userMessage) ? default : userMessage;

    public string UserMessage => userMessage ?? string.Empty;
}