using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentKernelFactory
{
    // Creates a kernel connected to the chat model. Tools are added by the caller
    Kernel Create();
}
