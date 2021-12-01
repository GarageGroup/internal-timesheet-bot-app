using System.Globalization;

namespace GGroupp.Infra.Bot.Builder;

partial class CardActionValueJson
{
    public string Serialize() => string.Format(CultureInfo.InvariantCulture, JsonSerializationFormat, Id);
}