using System;
using System.ClientModel.Primitives;
using Azure.Core;
using Microsoft.SemanticKernel;
using OpenAI;

namespace GarageGroup.Internal.Timesheet;

internal sealed class FoundryAgentKernelFactory : IAgentKernelFactory
{
    private readonly string modelId;

    private readonly OpenAIClient openAiClient;

    internal FoundryAgentKernelFactory(AgentFoundryOption option, TokenCredential tokenCredential)
    {
        modelId = option.ModelId;

#pragma warning disable OPENAI001
        openAiClient = new(
            authenticationPolicy: new BearerTokenPolicy(tokenCredential, option.TokenScope),
            options: new()
            {
                Endpoint = new($"{option.ProjectEndpoint.AbsoluteUri.TrimEnd('/')}/openai/v1/")
            });
#pragma warning restore OPENAI001
    }

    public Kernel Create()
    {
        var builder = Kernel.CreateBuilder();
        _ = builder.AddOpenAIChatCompletion(modelId, openAiClient);

        return builder.Build();
    }
}
