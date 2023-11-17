using System.Linq;
using System.Text;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class DataverseProjectSearch
{
    internal static string? GetProjectName(this DataverseSearchItem item, TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Project => item.ExtensionData.AsEnumerable().GetValueOrAbsent("gg_name").OrDefault()?.ToString(),
            TimesheetProjectType.Lead => item.BuildLeadProjectName(),
            TimesheetProjectType.Opportunity => item.ExtensionData.AsEnumerable().GetValueOrAbsent("name").OrDefault()?.ToString(),
            _ => item.ExtensionData.AsEnumerable().GetValueOrAbsent("title").OrDefault()?.ToString()
        };

    private static string? BuildLeadProjectName(this DataverseSearchItem item)
    {
        var companyName = item.ExtensionData.AsEnumerable().GetValueOrAbsent("companyname").OrDefault()?.ToString();
        var subject = item.ExtensionData.AsEnumerable().GetValueOrAbsent("subject").OrDefault()?.ToString();

        if (string.IsNullOrEmpty(companyName))
        {
            return subject;
        }

        var builder = new StringBuilder(subject);
        if (string.IsNullOrEmpty(subject) is false)
        {
            builder = builder.Append(' ');
        }

        return builder.Append('(').Append(companyName).Append(')').ToString();
    }
}