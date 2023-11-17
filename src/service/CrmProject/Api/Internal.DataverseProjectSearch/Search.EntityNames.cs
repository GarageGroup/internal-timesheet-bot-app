using System;

namespace GarageGroup.Internal.Timesheet;

partial class DataverseProjectSearch
{
    internal static readonly FlatArray<string> EntityNames
        =
        new(
            ProjectEntityName, LeadEntityName, OpportunityEntityName, IncidentEntityName);
}