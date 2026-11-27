using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

// Removes the stored refresh token of the Telegram user, so the agent can no longer act on the user's behalf
internal sealed class AgentSignOutCommand(AgentChatFeature chatFeature) : IChatCommand<AgentSignOutCommandIn, Unit>
{
    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentSignOutCommandIn, Unit> request, CancellationToken cancellationToken)
    {
        var localizer = request.Context.GetLocalizer(AgentMessageResource.BaseName);

        if (chatFeature.SignIn is not null)
        {
            var update = request.Context.Update;
            var userId = update.Message?.From?.Id ?? update.User.Id;

            await chatFeature.SignIn.SignOutAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        CultureMiddleware.Invalidate(request.Context.Update.Chat.Id);

        _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
            localizer[AgentMessageResource.SignedOut], cancellationToken).ConfigureAwait(false);

        return request.Context.CreateCompleteResult<Unit>(default);
    }
}
