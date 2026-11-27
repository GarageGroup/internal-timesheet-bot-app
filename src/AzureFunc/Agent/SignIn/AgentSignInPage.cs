using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentSignInPage
{
    internal static string GetLocale(string? locale)
        =>
        locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) is true ? "en" : "ru";

    internal static string BuildConfirmation(AgentPendingSignIn pending, string code)
    {
        var isEnglish = GetLocale(pending.Locale) is "en";
        var name = WebUtility.HtmlEncode(pending.UserDisplayName);

        var title = isEnglish ? "Confirm sign-in" : "Подтвердите вход";
        var text = isEnglish
            ? $"You are giving the Telegram account <b>{name}</b> access to the time tracking service on your behalf."
            : $"Вы даёте Telegram-аккаунту <b>{name}</b> доступ к сервису учёта времени от вашего имени.";
        var warning = isEnglish
            ? "If this is not your Telegram account, close this page."
            : "Если это не ваш Telegram-аккаунт, закройте эту страницу.";
        var button = isEnglish ? "Confirm" : "Подтвердить";

        var body =
            $"<p>{text}</p><p>{warning}</p>" +
            "<form method=\"post\">" +
            $"<input type=\"hidden\" name=\"state\" value=\"{WebUtility.HtmlEncode(pending.State)}\"/>" +
            $"<input type=\"hidden\" name=\"code\" value=\"{WebUtility.HtmlEncode(code)}\"/>" +
            $"<input type=\"hidden\" name=\"{AgentSignInCallbackHandler.ConfirmField}\" value=\"1\"/>" +
            $"<button type=\"submit\">{button}</button></form>";

        return BuildPage(isEnglish ? "en" : "ru", title, body);
    }

    // The page opens Telegram by itself and stays visible, so the result is clear even if the application does not open
    internal static string BuildSuccess(string locale, string? botUsername)
    {
        var isEnglish = GetLocale(locale) is "en";
        var title = isEnglish ? "You are signed in" : "Вход выполнен";
        var text = isEnglish ? "Return to Telegram: the bot will continue your request." : "Вернитесь в Telegram: бот продолжит ваш запрос.";

        if (IsValidBotUsername(botUsername) is false)
        {
            return BuildPage(isEnglish ? "en" : "ru", title, $"<p>{text}</p>");
        }

        var button = isEnglish ? "Open Telegram" : "Открыть Telegram";
        var body = $"<p>{text}</p><p><a class=\"button\" href=\"https://t.me/{botUsername}\">{button}</a></p>";

        return BuildPage(
            isEnglish ? "en" : "ru", title, body, head: $"<meta http-equiv=\"refresh\" content=\"1;url=tg://resolve?domain={botUsername}\"/>");
    }

    // Telegram usernames consist of latin letters, digits and underscores
    private static bool IsValidBotUsername([NotNullWhen(true)] string? botUsername)
    {
        if (string.IsNullOrEmpty(botUsername) || botUsername.Length > 64)
        {
            return false;
        }

        foreach (var symbol in botUsername)
        {
            if (char.IsAsciiLetterOrDigit(symbol) is false && symbol is not '_')
            {
                return false;
            }
        }

        return true;
    }

    internal static string BuildFailure(string locale)
        =>
        GetLocale(locale) is "en"
            ? BuildPage("en", "Sign-in failed", "<p>The link is invalid or has expired. Send a message to the bot to get a new one.</p>")
            : BuildPage("ru", "Не удалось войти", "<p>Ссылка недействительна или устарела. Напишите боту, чтобы получить новую.</p>");

    private static string BuildPage(string language, string title, string body, string head = "")
        =>
        $"<!DOCTYPE html><html lang=\"{language}\"><head><meta charset=\"utf-8\"/>" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>" +
        "<meta name=\"referrer\" content=\"no-referrer\"/>" +
        $"{head}<title>{title}</title>" +
        "<style>body{font-family:system-ui,sans-serif;max-width:32rem;margin:3rem auto;padding:0 1rem;line-height:1.5}" +
        "button,a.button{font-size:1rem;padding:.6rem 1.2rem;cursor:pointer}" +
        "a.button{display:inline-block;border:1px solid;border-radius:.3rem;text-decoration:none}</style></head>" +
        $"<body><h1>{title}</h1>{body}</body></html>";
}
