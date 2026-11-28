using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

// Runs around every tool call requested by the model. A call is executed right away only if its function is listed
// as not requiring confirmation. Any other call is captured instead. The model may request several calls in one answer:
// all of them are captured, and the model turn ends after the last one.
// Results of the executed calls are scanned for names of objects and for time entries
internal sealed class AgentToolInvocationFilter(
    IReadOnlySet<string> noConfirmationFunctions,
    IReadOnlySet<string> userSettingsFunctions,
    AgentEntityCache entityCache,
    long telegramUserId)
    : IAutoFunctionInvocationFilter
{
    internal const string PreparedResult =
        "The action has been prepared and is shown to the user for confirmation. It has NOT been executed yet.";

    // The preview and the stored action have size limits, so the number of calls prepared at once is limited too
    internal const int MaxCapturedCalls = 25;

    internal const string TooManyCallsResult =
        "Too many actions were requested at once. NONE of them has been prepared or executed. " +
        "Do not call these functions again in this turn. Tell the user to split the request into smaller parts.";

    private readonly List<AgentActionCall> capturedCalls = [];

    private bool isLimitExceeded;

    public IReadOnlyList<AgentActionCall> CapturedCalls
        =>
        capturedCalls;

    // Time entries returned by the tools in this turn: the answer shows them instead of their copy made by the model
    public AgentTimesheetSnapshot Timesheets { get; } = new();

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

            var resultText = context.Result.ToString();
            entityCache.Remember(telegramUserId, resultText);
            Timesheets.Remember(context.Arguments, resultText);

            UserSettingsChanged |= userSettingsFunctions.Contains(GetKey(function.PluginName, function.Name));

            // The calls are processed one by one, so the turn ends only after the last call of the model answer
            context.Terminate = capturedCalls.Count > 0 && IsLastCall(context);
            return;
        }

        if (isLimitExceeded || capturedCalls.Count >= MaxCapturedCalls)
        {
            // The model gets the refusal for every call and explains it to the user in its answer
            isLimitExceeded = true;
            capturedCalls.Clear();

            context.Result = new(function, TooManyCallsResult);
            return;
        }

        var arguments = (context.Arguments ?? [])
            .Where(static argument => argument.Value is not null)
            .ToDictionary(static argument => argument.Key, static argument => ToJsonElement(argument.Value!), StringComparer.Ordinal);

        capturedCalls.Add(
            new(
                ToolName: function.PluginName.OrEmpty(),
                FunctionName: function.Name,
                ArgumentsJson: JsonSerializer.Serialize(arguments),
                Arguments: [.. arguments.Select(argument => CreateArgument(argument.Key, argument.Value))]));

        context.Result = new(function, PreparedResult);

        // The preview is built by the bot from the captured arguments, so another model request is not needed
        context.Terminate = IsLastCall(context);
    }

    private static bool IsLastCall(AutoFunctionInvocationContext context)
        =>
        context.FunctionSequenceIndex >= context.FunctionCount - 1;

    private AgentActionArgument CreateArgument(string name, JsonElement value)
    {
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString().OrEmpty(),
            JsonValueKind.Number => value.GetDecimal().ToString("0.##", CultureInfo.InvariantCulture),
            _ => value.GetRawText()
        };

        var entity = value.ValueKind is JsonValueKind.String ? entityCache.Find(telegramUserId, text) : null;

        return new(name, text, entity?.Display)
        {
            Details = entity?.Details
        };
    }

    private static JsonElement ToJsonElement(object value)
        =>
        value is JsonElement element ? element.Clone() : JsonSerializer.SerializeToElement(value, value.GetType());
}
