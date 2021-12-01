using System;

namespace GGroupp.Infra.Bot.Builder;

internal sealed partial class CardActionValueJson
{
    public static Optional<CardActionValueJson> DeserializeOrAbsent(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return default;
        }

        var jsonMatch = JsonRegex.Match(text);
        if (jsonMatch.Success is false)
        {
            return default;
        }

        var guidValue = jsonMatch.Groups[1].Value;
        var id = Guid.Parse(guidValue);

        var valueJson = new CardActionValueJson(id);
        return Optional.Present(valueJson);
    }
}