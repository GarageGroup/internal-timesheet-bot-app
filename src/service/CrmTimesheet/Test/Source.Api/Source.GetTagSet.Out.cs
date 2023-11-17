using System;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static IEnumerable<object[]> OutputGetTagSetTestData
        =>
        [
            [
                default(FlatArray<DbTimesheetTag>),
                default(TimesheetTagSetGetOut)
            ],
            [
                new DbTimesheetTag[]
                {
                    new()
                    {
                        Description = null
                    },
                    new()
                    {
                        Description = "Some text without tags"
                    }
                },
                default(TimesheetTagSetGetOut)
            ],
            [
                new DbTimesheetTag[]
                {
                    new()
                    {
                        Description = "#Task1. Some first text"
                    },
                    new()
                    {
                        Description = string.Empty
                    },
                    new()
                    {
                        Description = "Some text.#Task_01#Task02, Some second # text"
                    },
                    new()
                    {
                        Description = "#SomeTag"
                    },
                    new()
                    {
                        Description = "#Task1; Another text"
                    },
                    new()
                    {
                        Description = null
                    },
                    new()
                    {
                        Description = "Text#One"
                    }
                },
                new TimesheetTagSetGetOut
                {
                    Tags = new("#Task1", "#Task_01", "#Task02", "#SomeTag", "#One")
                }
            ]
        ];
}