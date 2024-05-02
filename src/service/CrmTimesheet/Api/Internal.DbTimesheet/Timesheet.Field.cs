using GarageGroup.Infra;
using System;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheet
{
    [DbSelect(All, AliasName, $"{AliasName}.gg_duration")]
    public decimal Duration { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.regardingobjectid")]
    public Guid ProjectId { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.regardingobjecttypecode")]
    public int ProjectTypeCode { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.regardingobjectidname")]
    public string? ProjectName { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.subject")]
    public string? Subject { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.gg_description")]
    public string? Description { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.activityid")]
    public Guid Id { get; init; }

    [DbSelect(All, IncidentAlias, $"{IncidentAlias}.statecode")]
    public StateCode? IncidentStateCode { get; init; }

    [DbSelect(All, AliasName, $"{AliasName}.statecode")]
    public StateCode TimesheetStateCode { get; init; }
}