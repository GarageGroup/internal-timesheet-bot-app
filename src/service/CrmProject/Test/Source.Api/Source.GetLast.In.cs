using System;
using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiSource
{
    public static IEnumerable<object[]> InputGetLastTestData
        =>
        [
            [
                new LastProjectSetGetIn(
                    userId: Guid.Parse("bef33be0-99f5-4018-ba80-3366ec9ec1fd"),
                    top: 7,
                    minDate: new(2023, 05, 27)),
                new DbSelectQuery("gg_timesheetactivity", "t")
                {
                    Top = 7,
                    SelectedFields = new(
                        "t.regardingobjectid AS ProjectId",
                        "t.regardingobjecttypecode AS ProjectTypeCode",
                        "MAX(t.regardingobjectidname) AS ProjectName",
                        "MAX(t.subject) AS Subject",
                        "MAX(t.gg_date) AS MaxDate",
                        "MAX(t.createdon) AS MaxCreatedOn"),
                    Filter = new DbCombinedFilter(DbLogicalOperator.And)
                    {
                        Filters = new IDbFilter[]
                        {
                            new DbParameterFilter(
                                fieldName: "t.ownerid",
                                @operator: DbFilterOperator.Equal,
                                fieldValue: Guid.Parse("bef33be0-99f5-4018-ba80-3366ec9ec1fd"),
                                parameterName: "ownerId"),
                            new DbParameterFilter(
                                fieldName: "t.gg_date",
                                @operator: DbFilterOperator.GreaterOrEqual,
                                fieldValue: "2023-05-27",
                                parameterName: "minDate"),
                            new DbParameterArrayFilter(
                                fieldName: "t.regardingobjecttypecode",
                                @operator: DbArrayFilterOperator.In,
                                fieldValues: new(3, 4, 112, 10912),
                                parameterPrefix: "projectTypeCode")
                        }
                    },
                    GroupByFields = new(
                        "t.regardingobjectid",
                        "t.regardingobjecttypecode"),
                    Orders = new DbOrder[]
                    {
                        new("MaxDate", DbOrderType.Descending),
                        new("MaxCreatedOn", DbOrderType.Descending)
                    }
                }
            ]
        ];
}