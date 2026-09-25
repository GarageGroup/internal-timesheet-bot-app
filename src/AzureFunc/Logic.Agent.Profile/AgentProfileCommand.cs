using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentProfileCommand(IAgentProfileApi agentApi)
    : IChatCommand<AgentProfileCommandIn, Unit>, IChatCommandParser<AgentProfileCommandIn>
{
    public Optional<AgentProfileCommandIn> Parse(ChatUpdate update)
        =>
        AgentProfileCommandIn.Instance;

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentProfileCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        var chatId = request.Context.Update.Chat.Id;

        try
        {
            var profile = await agentApi.GetProfileAsync(chatId, chatId, cancellationToken).ConfigureAwait(false);
            var userName = WebUtility.HtmlEncode(profile.UserName);
            var languageCode = WebUtility.HtmlEncode(profile.LanguageCode);
            var text = $"Agent API: OK\nПользователь: {userName}\nЯзык: {languageCode}";

            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(text, cancellationToken).ConfigureAwait(false);
            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentProfileApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                "Профиль не найден. Откройте Mini App и выполните вход.",
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception ex)
        {
            request.Context.GetLogger<AgentProfileCommand>().LogError(ex, "Agent profile diagnostic request failed");
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                "Не удалось проверить подключение к Agent API.",
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCancelledResult<Unit>();
        }
    }
}
