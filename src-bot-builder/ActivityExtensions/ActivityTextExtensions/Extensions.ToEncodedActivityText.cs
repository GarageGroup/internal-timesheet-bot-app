using System.Diagnostics.CodeAnalysis;

namespace GGroupp.Infra.Bot.Builder;

public static partial class ActivityTextExtensions
{
    public static string ToEncodedActivityText([AllowNull] this string source)
        =>
        source?.InnerToEncodedActivityText() ?? string.Empty;

    private static string InnerToEncodedActivityText(this string source)
        =>
        regEscapeSymbols.Replace(source, SymbolsToReplace);
}