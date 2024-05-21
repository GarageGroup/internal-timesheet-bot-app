using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

public interface ITimesheetDeleteCommand : IChatCommand<TimesheetDeleteCommandIn, Unit>, IChatCommandParser<TimesheetDeleteCommandIn>;