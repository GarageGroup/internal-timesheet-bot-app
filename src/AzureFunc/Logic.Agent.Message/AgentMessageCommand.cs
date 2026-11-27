using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static AgentMessageResource;

internal sealed class AgentMessageCommand(
    IAgentMessageApi agentApi,
    IAgentVoiceFileApi voiceFileApi,
    AgentVoiceOption voiceOption,
    AgentChatFeature chatFeature)
    : IChatCommand<AgentMessageCommandIn, Unit>, IChatCommandParser<AgentMessageCommandIn>
{
    private static readonly Regex TelegramHtmlTagRegex = new(
        "</?(?:b|i|code)>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
        var localizer = request.Context.GetLocalizer(BaseName);

        try
        {
            var input = request.Value;

            if (chatFeature.Enabled)
            {
                return await SendChatReplyAsync(request, localizer, cancellationToken).ConfigureAwait(false);
            }

            var response = string.IsNullOrWhiteSpace(input.VoiceFileId)
                ? await SendTextMessageAsync(input, cancellationToken).ConfigureAwait(false)
                : await SendVoiceMessageAsync(request.Context, input, cancellationToken).ConfigureAwait(false);

            var preparedAction = GetPreparedAction(response);

            BotReplyMarkupBase replyMarkup = preparedAction is null
                ? new BotReplyKeyboardRemove()
                : BuildActionKeyboard(preparedAction.Value.ActionId, preparedAction.Value.ActionType, localizer);

            var messageText = BuildMessageText(response, localizer);
            var richMessageHtml = BuildRichMessageHtml(response, localizer);

            if (richMessageHtml is null)
            {
                _ = await request.Context.Api.SendMessageAsync(
                    new(messageText)
                    {
                        ParseMode = BotParseMode.Html,
                        ReplyMarkup = replyMarkup
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _ = await request.Context.Api.SendRichHtmlOrFallbackAsync(
                    richMessageHtml,
                    messageText,
                    replyMarkup,
                    request.Context.GetLogger<AgentMessageCommand>(),
                    cancellationToken).ConfigureAwait(false);
            }

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentUserNotSignedInException)
        {
            await SendSignInLinkAsync(request, localizer, cancellationToken).ConfigureAwait(false);
            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentChatException exception) when (exception.FailureCode is AgentChatFailureCode.MessageTooLong)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[MessageTooLong],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[ProfileNotFound],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.Conflict)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[ConversationConflict],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentVoiceFileTooLargeException)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[VoiceTooLarge],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentVoiceRecognitionException exception)
        {
            request.Context.GetLogger<AgentMessageCommand>().LogWarning(exception, "The voice message has not been recognized");
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[VoiceRecognitionError],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentVoiceFileException)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[VoiceDownloadError],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (
            string.IsNullOrWhiteSpace(request.Value.VoiceFileId) is false &&
            exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[VoiceRecognitionError],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentMessageCommand>().LogError(exception, "Agent message request failed");
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[UnexpectedError],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCancelledResult<Unit>();
        }
    }

    private async ValueTask<ChatCommandResult<Unit>> SendChatReplyAsync(
        ChatCommandRequest<AgentMessageCommandIn, Unit> request,
        IStringLocalizer localizer,
        CancellationToken cancellationToken)
    {
        var input = request.Value;
        var locale = request.Context.GetAgentLocale(input.Locale);
        var user = new AgentUserContext(input.TelegramUserId, input.TelegramChatId);

        var reply = await request.Context.WithTypingAsync(
            async token =>
            {
                var text = string.IsNullOrWhiteSpace(input.VoiceFileId)
                    ? input.Text
                    : await TranscribeVoiceAsync(request.Context, input, locale, token).ConfigureAwait(false);

                return await chatFeature.Service.SendAsync(new(user, text, locale), token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        await AgentChatReplySender.SendAsync(request.Context, reply, cancellationToken).ConfigureAwait(false);
        return request.Context.CreateCompleteResult<Unit>(default);
    }

    private async ValueTask SendSignInLinkAsync(
        ChatCommandRequest<AgentMessageCommandIn, Unit> request,
        IStringLocalizer localizer,
        CancellationToken cancellationToken)
    {
        if (chatFeature.SignIn is null)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[ProfileNotFound],
                cancellationToken).ConfigureAwait(false);

            return;
        }

        var input = request.Value;
        var user = request.Context.Update.Message?.From ?? request.Context.Update.User;

        var signInUrl = await chatFeature.SignIn.CreateSignInUrlAsync(
            new(
                User: new(input.TelegramUserId, input.TelegramChatId),
                UserDisplayName: GetDisplayName(user),
                Locale: request.Context.GetAgentLocale(input.Locale),
                SourceUpdateJson: JsonSerializer.Serialize(request.Context.Update)),
            cancellationToken).ConfigureAwait(false);

        var signInMessage = await request.Context.Api.SendMessageAsync(
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

    private ValueTask<AgentMessage> SendTextMessageAsync(
        AgentMessageCommandIn input,
        CancellationToken cancellationToken)
        =>
        agentApi.SendMessageAsync(
            input.TelegramUpdateId,
            input.TelegramUserId,
            input.TelegramChatId,
            input.Text,
            input.Locale.OrEmpty(),
            cancellationToken);

    // Recognizes a voice message with the speech-to-text model of the in-bot agent
    private async Task<string> TranscribeVoiceAsync(
        IChatContext context, AgentMessageCommandIn input, string locale, CancellationToken cancellationToken)
    {
        if (chatFeature.Voice is null)
        {
            throw new AgentVoiceRecognitionException("Voice messages are not configured for the in-bot agent.");
        }

        var (audio, fileName) = await DownloadVoiceAsync(context, input, cancellationToken).ConfigureAwait(false);

        return await chatFeature.Voice.TranscribeAsync(
            new(audio, GetMimeType(input.VoiceMimeType), fileName, locale), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<(byte[] Audio, string FileName)> DownloadVoiceAsync(
        IChatContext context, AgentMessageCommandIn input, CancellationToken cancellationToken)
    {
        ValidateVoiceSize(input.VoiceFileSize);

        var fileLink = await context.Api.GetFileLinkAsync(input.VoiceFileId, cancellationToken).ConfigureAwait(false);
        ValidateVoiceSize(fileLink.FileSize ?? 0);

        var audio = await voiceFileApi.DownloadAsync(
            fileLink.FileUrl,
            voiceOption.MaxFileSizeBytes,
            cancellationToken).ConfigureAwait(false);
        ValidateVoiceSize(audio.LongLength);

        return (audio, GetFileName(fileLink.FilePath));
    }

    private async ValueTask<AgentMessage> SendVoiceMessageAsync(
        IChatContext context,
        AgentMessageCommandIn input,
        CancellationToken cancellationToken)
    {
        var (audio, fileName) = await DownloadVoiceAsync(context, input, cancellationToken).ConfigureAwait(false);

        return await agentApi.SendVoiceMessageAsync(
            input.TelegramUpdateId,
            input.TelegramUserId,
            input.TelegramChatId,
            audio,
            GetMimeType(input.VoiceMimeType),
            fileName,
            GetLanguage(input.Locale.OrEmpty()),
            input.Locale.OrEmpty(),
            cancellationToken).ConfigureAwait(false);
    }

    private void ValidateVoiceSize(long fileSize)
    {
        if (fileSize > voiceOption.MaxFileSizeBytes)
        {
            throw new AgentVoiceFileTooLargeException();
        }
    }

    private static string GetFileName(string filePath)
    {
        var separatorIndex = filePath.LastIndexOf('/');

        return separatorIndex < 0 ? filePath : filePath[(separatorIndex + 1)..];
    }

    private static string GetMimeType(string mimeType)
        =>
        string.IsNullOrWhiteSpace(mimeType) ? "audio/ogg" : mimeType;

    private static string GetLanguage(string locale)
    {
        var separatorIndex = locale.IndexOfAny(['-', '_']);

        return separatorIndex < 0 ? locale : locale[..separatorIndex];
    }

    private static (Guid ActionId, AgentActionType ActionType)? GetPreparedAction(AgentMessage message)
    {
        var actionCount = new object?[]
        {
            message.PreparedCreateAction,
            message.PreparedDeleteAction,
            message.PreparedUpdateAction
        }.Count(static action => action is not null);

        if (actionCount > 1)
        {
            throw new InvalidOperationException("Agent response contains more than one prepared action");
        }

        if (message.PreparedCreateAction is not null)
        {
            return (message.PreparedCreateAction.ActionId, AgentActionType.Create);
        }

        if (message.PreparedDeleteAction is not null)
        {
            return (message.PreparedDeleteAction.ActionId, AgentActionType.Delete);
        }

        if (message.PreparedUpdateAction is not null)
        {
            return (message.PreparedUpdateAction.ActionId, AgentActionType.Update);
        }

        return null;
    }

    private static string BuildMessageText(AgentMessage message, IStringLocalizer localizer)
        =>
        BuildPreview(message, localizer, CreatePreview, DeletePreview, UpdatePreview) ?? SanitizeAgentHtml(message.Text);

    private static string? BuildRichMessageHtml(AgentMessage message, IStringLocalizer localizer)
        =>
        BuildPreview(message, localizer, CreatePreviewRich, DeletePreviewRich, UpdatePreviewRich);

    private static string? BuildPreview(
        AgentMessage message,
        IStringLocalizer localizer,
        string createTemplate,
        string deleteTemplate,
        string updateTemplate)
    {
        if (message.PreparedCreateAction is AgentPreparedCreateAction createAction)
        {
            return FormatPreview(
                localizer[createTemplate], createAction.Date, createAction.ProjectName, createAction.Duration, createAction.Description);
        }

        if (message.PreparedDeleteAction is AgentPreparedDeleteAction deleteAction)
        {
            return FormatPreview(
                localizer[deleteTemplate], deleteAction.Date, deleteAction.ProjectName, deleteAction.Duration, deleteAction.Description);
        }

        if (message.PreparedUpdateAction is AgentPreparedUpdateAction updateAction)
        {
            return FormatPreview(
                localizer[updateTemplate], updateAction.Date, updateAction.ProjectName, updateAction.Duration, updateAction.Description);
        }

        return null;
    }

    private static string FormatPreview(string template, DateOnly date, string projectName, decimal duration, string description)
        =>
        string.Format(
            CultureInfo.CurrentCulture,
            template,
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            WebUtility.HtmlEncode(projectName),
            duration.ToString("0.##", CultureInfo.CurrentCulture),
            WebUtility.HtmlEncode(description));

    private static string SanitizeAgentHtml(string text)
    {
        var builder = new StringBuilder(text.Length);
        var currentIndex = 0;

        foreach (Match match in TelegramHtmlTagRegex.Matches(text))
        {
            _ = builder
                .Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(text[currentIndex..match.Index])))
                .Append(match.Value);
            currentIndex = match.Index + match.Length;
        }

        return builder
            .Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(text[currentIndex..])))
            .ToString();
    }

    private static BotInlineKeyboardMarkup BuildActionKeyboard(
        Guid actionId,
        AgentActionType actionType,
        IStringLocalizer localizer)
        =>
        new()
        {
            InlineKeyboard =
            [
                [
                    new(localizer[ConfirmButton])
                    {
                        CallbackData = AgentActionCommand.BuildCallbackData(actionId, actionType, AgentActionDecision.Confirm)
                    },
                    new(localizer[CancelButton])
                    {
                        CallbackData = AgentActionCommand.BuildCallbackData(actionId, actionType, AgentActionDecision.Cancel)
                    }
                ]
            ]
        };
}
