using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static AgentProfileResource;

internal sealed class AgentProfileCommand(IAgentProfileApi agentApi)
    : IChatCommand<AgentProfileCommandIn, Unit>
{
    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentProfileCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        var localizer = request.Context.GetLocalizer(BaseName);
        var chatId = request.Context.Update.Chat.Id;

        try
        {
            var profile = await agentApi.GetProfileAsync(chatId, chatId, cancellationToken).ConfigureAwait(false);
            var userName = WebUtility.HtmlEncode(profile.UserName);
            var languageCode = WebUtility.HtmlEncode(profile.LanguageCode);
            var text = localizer.GetString(ProfileTemplate, userName, languageCode);

            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(text, cancellationToken).ConfigureAwait(false);
            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentProfileApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[ProfileNotFound],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception ex)
        {
            request.Context.GetLogger<AgentProfileCommand>().LogError(ex, "Agent profile diagnostic request failed");
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[UnexpectedError],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCancelledResult<Unit>();
        }
    }
}
