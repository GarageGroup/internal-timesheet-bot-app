using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using static AgentMessageResource;

// Passes a text or voice message of the user to the agent and sends the answer
internal sealed class AgentMessageCommand(IAgentVoiceFileApi voiceFileApi, AgentVoiceOption voiceOption, AgentChatFeature chatFeature)
    : IChatCommand<AgentMessageCommandIn, Unit>, IChatCommandParser<AgentMessageCommandIn>
{
    public Optional<AgentMessageCommandIn> Parse(ChatUpdate update)
    {
        var message = update.Message;
        if (message is null)
        {
            return default;
        }

        var hasText = string.IsNullOrWhiteSpace(message.Text) is false &&
            message.Entities.AsEnumerable().Any(static entity => entity.Type is BotMessageEntityType.BotCommand) is false;
        var voice = message.Voice;

        if (hasText is false && voice is null)
        {
            return default;
        }

        var userId = message.From?.Id ?? update.User.Id;

        return new AgentMessageCommandIn(
            update.UpdateId,
            userId,
            update.Chat.Id,
            hasText ? message.Text.OrEmpty() : string.Empty,
            voice?.FileId.OrEmpty() ?? string.Empty,
            voice?.MimeType.OrEmpty() ?? string.Empty,
            voice?.FileSize ?? 0,
            update.User.LanguageCode.OrEmpty());
    }

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentMessageCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        var context = request.Context;
        var input = request.Value;
        var localizer = context.GetLocalizer(BaseName);

        try
        {
            var locale = context.GetAgentLocale(input.Locale);
            var user = new AgentUserContext(input.TelegramUserId, input.TelegramChatId);

            var reply = await context.WithTypingAsync(
                async token =>
                {
                    var text = string.IsNullOrWhiteSpace(input.VoiceFileId)
                        ? input.Text
                        : await TranscribeVoiceAsync(context, input, locale, token).ConfigureAwait(false);

                    return await chatFeature.Service.SendAsync(new(user, text, locale), token).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);

            await AgentChatReplySender.SendAsync(context, reply, locale, cancellationToken).ConfigureAwait(false);
            return context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentUserNotSignedInException)
        {
            await SendSignInLinkAsync(request, localizer, cancellationToken).ConfigureAwait(false);
            return context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentChatException exception) when (exception.FailureCode is AgentChatFailureCode.MessageTooLong)
        {
            return await SendFailureAsync(context, localizer[MessageTooLong], cancellationToken).ConfigureAwait(false);
        }
        catch (AgentVoiceFileTooLargeException)
        {
            return await SendFailureAsync(context, localizer[VoiceTooLarge], cancellationToken).ConfigureAwait(false);
        }
        catch (AgentVoiceFileException)
        {
            return await SendFailureAsync(context, localizer[VoiceDownloadError], cancellationToken).ConfigureAwait(false);
        }
        catch (AgentVoiceRecognitionException exception)
        {
            context.GetLogger<AgentMessageCommand>().LogWarning(exception, "The voice message has not been recognized");
            return await SendFailureAsync(context, localizer[VoiceRecognitionError], cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.GetLogger<AgentMessageCommand>().LogError(exception, "The agent request has failed");
            _ = await context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(localizer[UnexpectedError], cancellationToken).ConfigureAwait(false);

            return context.CreateCancelledResult<Unit>();
        }
    }

    private static async ValueTask<ChatCommandResult<Unit>> SendFailureAsync(
        IChatContext context, string text, CancellationToken cancellationToken)
    {
        _ = await context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(text, cancellationToken).ConfigureAwait(false);
        return context.CreateCompleteResult<Unit>(default);
    }

    private async ValueTask SendSignInLinkAsync(
        ChatCommandRequest<AgentMessageCommandIn, Unit> request,
        IStringLocalizer localizer,
        CancellationToken cancellationToken)
    {
        var context = request.Context;
        if (chatFeature.SignIn is null)
        {
            _ = await context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(localizer[SignInUnavailable], cancellationToken).ConfigureAwait(false);
            return;
        }

        var input = request.Value;
        var user = context.Update.Message?.From ?? context.Update.User;

        var signInUrl = await chatFeature.SignIn.CreateSignInUrlAsync(
            new(
                User: new(input.TelegramUserId, input.TelegramChatId),
                UserDisplayName: GetDisplayName(user),
                Locale: context.GetAgentLocale(input.Locale),
                SourceUpdateJson: JsonSerializer.Serialize(context.Update)),
            cancellationToken).ConfigureAwait(false);

        var signInMessage = await context.Api.SendMessageAsync(
            new(localizer[SignInRequired])
            {
                ParseMode = BotParseMode.Html,
                ReplyMarkup = new BotInlineKeyboardMarkup
                {
                    InlineKeyboard =
                    [
                        [
                            new(localizer[SignInButton])
                            {
                                Url = signInUrl.AbsoluteUri
                            }
                        ]
                    ]
                }
            },
            cancellationToken).ConfigureAwait(false);

        await chatFeature.SignIn.SetSignInMessageAsync(signInUrl, signInMessage.MessageId, cancellationToken).ConfigureAwait(false);
    }

    private static string GetDisplayName(BotUser user)
    {
        var name = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(static part => string.IsNullOrWhiteSpace(part) is false));
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            return name;
        }

        return string.IsNullOrWhiteSpace(name) ? "@" + user.Username : $"{name} (@{user.Username})";
    }

    private async Task<string> TranscribeVoiceAsync(
        IChatContext context, AgentMessageCommandIn input, string locale, CancellationToken cancellationToken)
    {
        if (chatFeature.Voice is null)
        {
            throw new AgentVoiceRecognitionException("Voice messages are not configured for the agent.");
        }

        ValidateVoiceSize(input.VoiceFileSize);

        var fileLink = await context.Api.GetFileLinkAsync(input.VoiceFileId, cancellationToken).ConfigureAwait(false);
        ValidateVoiceSize(fileLink.FileSize ?? 0);

        var audio = await voiceFileApi.DownloadAsync(fileLink.FileUrl, voiceOption.MaxFileSizeBytes, cancellationToken).ConfigureAwait(false);
        ValidateVoiceSize(audio.LongLength);

        var mimeType = string.IsNullOrWhiteSpace(input.VoiceMimeType) ? "audio/ogg" : input.VoiceMimeType;
        var separatorIndex = fileLink.FilePath.LastIndexOf('/');
        var fileName = separatorIndex < 0 ? fileLink.FilePath : fileLink.FilePath[(separatorIndex + 1)..];

        return await chatFeature.Voice.TranscribeAsync(new(audio, mimeType, fileName, locale), cancellationToken).ConfigureAwait(false);
    }

    private void ValidateVoiceSize(long fileSize)
    {
        if (fileSize > voiceOption.MaxFileSizeBytes)
        {
            throw new AgentVoiceFileTooLargeException();
        }
    }
}
