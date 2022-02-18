using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaptiveCards;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace GGroupp.Internal.Timesheet;

partial class BotMenuActivity
{
    internal static IActivity CreateMenuActivity(this ITurnContext turnContext, BotMenuData menuData)
    {

    }

    private static string BuildTelegramText(this ITurnContext turnContext, BotMenuData menuData)
    {
        var encodedText = turnContext.EncodeText(menuData.Text);
        if (menuData.Commands.Any() is false)
        {
            return encodedText;
        }

        var textBuilder = new StringBuilder(encodedText);

        foreach (var command in menuData.Commands)
        {
            if (textBuilder.Length is not 0)
            {
                textBuilder.Append("\n\r\n\r");
            }

            var encodedCommandName = turnContext.EncodeText(command.Name);
            var encodedCommandDescription = turnContext.EncodeText(command.Description);

            if (string.IsNullOrEmpty(encodedCommandName) is false)
            {
                textBuilder.Append("/" + encodedCommandName);

                if (string.IsNullOrEmpty(encodedCommandDescription) is false)
                {
                    textBuilder.Append(" - ");
                }
            }

            textBuilder.Append(encodedCommandDescription);
        }

        return textBuilder.ToString();
    }

    private static IActivity CreateAdaptiveCardActivity(ITurnContext context, BotMenuData menuData)
        =>
        new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.GetAdaptiveSchemaVersion())
            {
                Body = new()
                {
                    new AdaptiveTextBlock
                    {
                        Text = menuData.Text,
                        Weight = AdaptiveTextWeight.Bolder,
                        Wrap = true
                    }
                },
                Actions = menuData.Commands.Where(HasDescription).Select(context.CreateAdaptiveSubmitAction).ToList<AdaptiveAction>()
            }
        }
        .ToActivity();

    private static IActivity CreateHeroCardActivity(ITurnContext turnContext, BotMenuData menuData)
        =>
        new HeroCard
        {
            Title = menuData.Text,
            Buttons = menuData.Commands.Where(HasDescription).Select(turnContext.CreateCommandAction).ToArray()
        }
        .ToAttachment()
        .ToActivity();

    private static List<AdaptiveElement> CreateBody(BotMenuData menuData)
    {
        if (string.IsNullOrEmpty(menuData.Text))
        {
            return Enumerable.Empty<AdaptiveElement>().ToList();
        }

        return new()
        {
            new AdaptiveTextBlock
            {
                Text = menuData.Text,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true
            }
        };
    }

    private static AdaptiveSubmitAction CreateAdaptiveSubmitAction(this ITurnContext turnContext, BotMenuCommand command)
        =>
        new()
        {
            Title = command.Description,
            Data = turnContext.BuildCardActionValue(command.Id)
        };

    private static CardAction CreateCommandAction(this ITurnContext turnContext, BotMenuCommand command)
        =>
        new(ActionTypes.PostBack)
        {
            Title = command.Description,
            Text = command.Description,
            Value = turnContext.BuildCardActionValue(command.Id)
        };

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);

    private static bool HasDescription(BotMenuCommand menuCommand)
        =>
        string.IsNullOrEmpty(menuCommand.Description) is false;
}