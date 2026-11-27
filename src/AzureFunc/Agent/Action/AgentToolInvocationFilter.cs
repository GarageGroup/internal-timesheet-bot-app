using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentCapturedCall(string ToolName, string FunctionName, string ArgumentsJson, AgentActionArgument[] Arguments);

// Runs around every tool call requested by the model. A call that requires confirmation is not executed:
// it is captured, and the model turn ends. Results of the other calls are scanned for names of objects
internal sealed class AgentToolInvocationFilter(
    IReadOnlySet<string> confirmationFunctions, AgentEntityCache entityCache, long telegramUserId)
    : IAutoFunctionInvocationFilter
{
    internal const string PreparedResult =
        "The action has been prepared and is shown to the user for confirmation. It has NOT been executed yet.";

    public AgentCapturedCall? CapturedCall { get; private set; }

    internal static string GetKey(string? toolName, string functionName)
        =>
        $"{toolName}.{functionName}";

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        var function = context.Function;

        if (confirmationFunctions.Contains(GetKey(function.PluginName, function.Name)) is false)
        {
            await next(context).ConfigureAwait(false);
            entityCache.Remember(telegramUserId, context.Result.ToString());

            return;
        }

        var arguments = (context.Arguments ?? [])
            .Where(static argument => argument.Value is not null)
            .ToDictionary(static argument => argument.Key, static argument => ToJsonElement(argument.Value!), StringComparer.Ordinal);

        CapturedCall = new(
            ToolName: function.PluginName.OrEmpty(),
            FunctionName: function.Name,
            ArgumentsJson: JsonSerializer.Serialize(arguments),
            Arguments: [.. arguments.Select(argument => CreateArgument(argument.Key, argument.Value))]);

        context.Result = new(function, PreparedResult);

        // The preview is built by the bot from the captured arguments, so another model request is not needed
        context.Terminate = true;
    }

    private AgentActionArgument CreateArgument(string name, JsonElement value)
    {
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString().OrEmpty(),
            JsonValueKind.Number => value.GetDecimal().ToString("0.##", CultureInfo.InvariantCulture),
            _ => value.GetRawText()
        };

        return new(name, text, value.ValueKind is JsonValueKind.String ? entityCache.FindDisplay(telegramUserId, text) : null);
    }

    private static JsonElement ToJsonElement(object value)
        =>
        value is JsonElement element ? element.Clone() : JsonSerializer.SerializeToElement(value, value.GetType());
}
