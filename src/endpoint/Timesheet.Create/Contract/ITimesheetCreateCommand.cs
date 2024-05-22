using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

public interface ITimesheetCreateCommand : IChatCommand<TimesheetCreateCommandIn, Unit>, IChatCommandParser<TimesheetCreateCommandIn>;