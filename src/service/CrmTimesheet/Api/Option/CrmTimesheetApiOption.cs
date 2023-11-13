using System;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct CrmTimesheetApiOption
{
    public CrmTimesheetApiOption(FlatArray<KeyValuePair<TimesheetChannel, int?>> channelCodes)
        =>
        ChannelCodes = channelCodes;

    public FlatArray<KeyValuePair<TimesheetChannel, int?>> ChannelCodes { get; }
}