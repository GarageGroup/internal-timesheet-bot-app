using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

internal interface IOAuthFlowContext : ITurnContext
{
    ILogger GetLogger();
}