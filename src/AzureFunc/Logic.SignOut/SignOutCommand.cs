using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using ISignOutCommand = IChatCommand<SignOutCommandIn, Unit>;
using ISignOutCommandParser = IChatCommandParser<SignOutCommandIn>;

internal sealed class SignOutCommand(IUserUnauthorizeSupplier authorizationApi) : ISignOutCommand, ISignOutCommandParser
{
    public Optional<SignOutCommandIn> Parse(ChatUpdate update)
        =>
        update.MyChatMember?.NewChatMember switch
        {
            BotBannedChatMember => Optional.Present(SignOutCommandIn.Instance),
            _ => default
        };

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<SignOutCommandIn, Unit> request, CancellationToken cancellationToken)
    {
        var bot = await request.Context.Api.GetMeAsync(cancellationToken);

        var @in = new UserUnauthorizeIn(botId: bot.Id, chatId: request.Context.Update.Chat.Id);
        var result = await authorizationApi.UnauthorizeAsync(@in, cancellationToken);

        return result.Fold(OnSuccess, OnFailure);

        ChatCommandResult<Unit> OnSuccess(Unit _)
            =>
            request.Context.CreateCompleteResult<Unit>(default);

        ChatCommandResult<Unit> OnFailure(Failure<Unit> failure)
        {
            var logger = request.Context.GetLogger<SignOutCommand>();
            logger.LogError(failure.SourceException, "SignOut error: {failureMessage}", failure.FailureMessage);

            return request.Context.CreateCancelledResult<Unit>();
        }
    }
}