using System;
using GGroupp.Platform;

namespace GGroupp.Internal.Timesheet;

internal static partial class UserAuthorizeFlow
{
    private const string UserNotFoundFailureMessage
        =
        "Пользователь не найден. Возомжно у вас нет прав для доступа в систему";

    private const string UnexpectedFailureMessage
        =
        "Возникла непредвиденная ошибка при попытке получить данные пользователя. Повторите попытку позже или обратитесь к администратору";

    private static bool EqualsInvariant(this string? a, string? b)
        =>
        string.Equals(a, b, StringComparison.CurrentCultureIgnoreCase);
}