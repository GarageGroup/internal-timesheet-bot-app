using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentCapturedCall(string ToolName, string FunctionName, string ArgumentsJson, AgentActionArgument[] Arguments);

// Runs around every tool call requested by the model. A call is executed right away only if its function is listed
// as not requiring confirmation. Any other call is captured instead, and the model turn ends.
// Results of the executed calls are scanned for names of objects
internal sealed class AgentToolInvocationFilter(
    IReadOnlySet<string> noConfirmationFunctions,
    IReadOnlySet<string> userSettingsFunctions,
    AgentEntityCache entityCache,
    long telegramUserId)
    : IAutoFunctionInvocationFilter
{
    internal const string PreparedResult =
        "The action has been prepared and is shown to the user for confirmation. It has NOT been executed yet.";

    public AgentCapturedCall? CapturedCall { get; private set; }

    // True when a function that changes the user settings has been executed in this turn
    public bool UserSettingsChanged { get; private set; }

    internal const string AnyFunction = "*";

    internal static string GetKey(string? toolName, string functionName)
        =>
        $"{toolName}.{functionName}";

    // A function is executed without confirmation only if it is listed explicitly or the whole tool is listed with '*'
    internal static bool RequiresConfirmation(IReadOnlySet<string> noConfirmationFunctions, string? toolName, string functionName)
        =>
        noConfirmationFunctions.Contains(GetKey(toolName, functionName)) is false &&
        noConfirmationFunctions.Contains(GetKey(toolName, AnyFunction)) is false;

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        var function = context.Function;

        if (RequiresConfirmation(noConfirmationFunctions, function.PluginName, function.Name) is false)
        {
            await next(context).ConfigureAwait(false);
            entityCache.Remember(telegramUserId, context.Result.ToString());

            UserSettingsChanged |= userSettingsFunctions.Contains(GetKey(function.PluginName, function.Name));

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
