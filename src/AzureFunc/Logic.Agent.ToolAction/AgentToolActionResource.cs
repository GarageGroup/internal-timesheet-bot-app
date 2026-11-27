namespace GarageGroup.Internal.Timesheet;

internal static class AgentToolActionResource
{
    public const string BaseName = "AgentToolAction";

    // Prefixes of optional resources: '<prefix><function>', '<prefix><function>_<argument>', '<prefix><argument>', '<prefix><value>'
    public const string TitlePrefix = "Title_";

    public const string ArgumentPrefix = "Arg_";

    public const string ValuePrefix = "Value_";

    // '<prefix><argument>' names another argument whose value is used as the label of this one
    public const string LabelFromPrefix = "LabelFrom_";

    public const string DefaultTitle = "DefaultTitle";

    public const string PreviewHint = "PreviewHint";

    public const string ConfirmButton = "ConfirmButton";

    public const string CancelButton = "CancelButton";

    public const string Cancelled = "Cancelled";

    public const string NotFound = "NotFound";

    public const string Succeeded = "Succeeded";

    public const string Failed = "Failed";

    public const string SignInRequired = "SignInRequired";

    public const string UnexpectedError = "UnexpectedError";

    public const string TimesheetTotal = "TimesheetTotal";

    public const string TimesheetPeriodTotal = "TimesheetPeriodTotal";

    public const string HoursUnit = "HoursUnit";
}
