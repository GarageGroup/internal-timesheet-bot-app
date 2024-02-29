using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet
{
    public enum TimesheetDeleteFailureCode
    {
        Unknown,

        NotFound,

        TooManyRequests
    }
}
