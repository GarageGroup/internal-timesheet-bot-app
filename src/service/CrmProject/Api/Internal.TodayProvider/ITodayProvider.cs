using System;

namespace GarageGroup.Internal.Timesheet;

internal interface ITodayProvider
{
    DateOnly GetToday();
}