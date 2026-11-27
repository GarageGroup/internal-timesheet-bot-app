using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentToolLoader
{
    // Loads the configured tools on behalf of the user.
    // Throws AgentUserNotSignedInException if a tool requires a user access token and the user has not signed in
    ValueTask<AgentToolSet> LoadAsync(AgentUserContext user, CancellationToken cancellationToken);
}
