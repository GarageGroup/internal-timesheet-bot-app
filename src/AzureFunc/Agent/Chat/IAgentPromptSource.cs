using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentPromptSource
{
    // Returns the system prompt template with placeholders
    ValueTask<string> GetTemplateAsync(CancellationToken cancellationToken);
}

internal sealed class StaticAgentPromptSource(string template) : IAgentPromptSource
{
    public ValueTask<string> GetTemplateAsync(CancellationToken cancellationToken)
        =>
        new(template);
}
