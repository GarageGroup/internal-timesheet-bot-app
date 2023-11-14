using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static IEnumerable<object[]> OutputGetTagSetTestData
        =>
        [
            [
                default(DataverseEntitySetGetOut<TimesheetTagJson>),
                default(TimesheetTagSetGetOut)
            ],
            [
                new DataverseEntitySetGetOut<TimesheetTagJson>(
                    value: new TimesheetTagJson[]
                    {
                        new()
                        {
                            Description = null
                        },
                        new()
                        {
                            Description = "Some text without tags"
                        }
                    }),
                default(TimesheetTagSetGetOut)
            ],
            [
                new DataverseEntitySetGetOut<TimesheetTagJson>(
                    value: new TimesheetTagJson[]
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
                    }),
                new TimesheetTagSetGetOut
                {
                    Tags = new("#Task1", "#Task_01", "#Task02", "#SomeTag", "#One")
                }
            ]
        ];
}