using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GarageGroup.Internal.Timesheet;

// Applies the values the user has changed in the confirmation card to a prepared call.
// Only a single call that creates or changes a time entry can be edited
internal static class AgentActionEditor
{
    internal const string CreateFunctionName = "create-timesheet";

    internal const string UpdateFunctionName = "update-timesheet";

    internal const string DateArgumentName = "date";

    internal const string DurationArgumentName = "duration";

    internal const string DateFormat = "yyyy-MM-dd";

    // The duration of a time entry is a number of quarters of an hour
    internal const decimal DurationStep = 0.25m;

    internal const decimal MaxDuration = 24;

    internal static bool IsEditable(AgentPendingAction action)
        =>
        action.Calls is [{ FunctionName: CreateFunctionName or UpdateFunctionName }];

    internal static bool IsValidDuration(decimal duration)
        =>
        duration >= DurationStep && duration <= MaxDuration && duration % DurationStep is 0;

    // A value that cannot be a value of a time entry is ignored: the call keeps the value prepared by the model
    internal static AgentPendingAction Apply(AgentPendingAction action, AgentActionEdit? edit)
    {
        if (edit is null || IsEditable(action) is false)
        {
            return action;
        }

        var call = action.Calls[0];
        if (JsonNode.Parse(call.ArgumentsJson) is not JsonObject arguments)
        {
            return action;
        }

        var displayArguments = call.Arguments;

        if (edit.Date is DateOnly date)
        {
            var text = date.ToString(DateFormat, CultureInfo.InvariantCulture);

            arguments[DateArgumentName] = text;
            displayArguments = SetArgument(displayArguments, DateArgumentName, text);
        }

        if (edit.Duration is decimal duration && IsValidDuration(duration))
        {
            arguments[DurationArgumentName] = duration;
            displayArguments = SetArgument(displayArguments, DurationArgumentName, duration.ToString("0.##", CultureInfo.InvariantCulture));
        }

        return action with
        {
            Calls = [call with { ArgumentsJson = arguments.ToJsonString(), Arguments = displayArguments }]
        };
    }

    private static AgentActionArgument[] SetArgument(AgentActionArgument[] arguments, string name, string value)
        =>
        arguments.Any(argument => argument.Name == name)
            ? [.. arguments.Select(argument => argument.Name == name ? new AgentActionArgument(name, value) : argument)]
            : [.. arguments, new(name, value)];
}
