using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static WelcomeResource;

internal sealed class WelcomeCommand(WelcomeOption option) : IChatCommand<WelcomeCommandIn, Unit>, IChatCommandParser<WelcomeCommandIn>
{
    public Optional<WelcomeCommandIn> Parse(ChatUpdate update)
        =>
        WelcomeCommandIn.Instance;

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<WelcomeCommandIn, Unit> request, CancellationToken cancellationToken)
    {
        var localizer = request.Context.GetLocalizer(BaseName);

        var emoji = GetRandomEmoji(option.Emojis);
        var buttonName = localizer[ButtonTextMessageName];

        var welcomeMessage = new ChatPhotoSendRequest(photo: option.ImageUrl)
        {
            Caption = localizer.GetString(CaptionTemplateMessageName, emoji, buttonName),
            ReplyMarkup = request.Context.WebApp is null ? null : BuildReplyMarkup(request.Context.WebApp)
        };

        _ = await request.Context.Api.SendPhotoAsync(welcomeMessage, cancellationToken);
        return request.Context.CreateCompleteResult<Unit>(default);

        BotInlineKeyboardMarkup BuildReplyMarkup(ChatWebApp webApp)
            =>
            new()
            {
                InlineKeyboard =
                [
                    [
                        new(buttonName)
                        {
                            WebApp = new(webApp.BaseAddress.AbsoluteUri)
                        }
                    ]
                ]
            };

        static string GetRandomEmoji(FlatArray<string> emojis)
            =>
            emojis.IsEmpty ? string.Empty : emojis[Random.Shared.Next(emojis.Length)];
    }
}