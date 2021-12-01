using System.Text.RegularExpressions;

namespace GGroupp.Infra.Bot.Builder;

public static partial class ActivityTextExtensions
{
    private static readonly Regex regEscapeSymbols;

    private const string SymbolsToReplace = "\u2063\n\r\n\r\u2063";

    static ActivityTextExtensions()
        =>
        regEscapeSymbols = new("(?<!\\r)(\\n)(?!\\r)", RegexOptions.CultureInvariant);
}
