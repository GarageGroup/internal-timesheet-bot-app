using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

public interface ITimesheetShowCommand : IChatCommand<TimesheetShowCommandIn, Unit>;