using System;
using System.Collections.Generic;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    private static Type EqualityContract => typeof(ChatFlowAction<T>);

    private static IEqualityComparer<T> FlowStateComparer => EqualityComparer<T>.Default;

    private static IEqualityComparer<object?> StepStateComparer => EqualityComparer<object?>.Default;

    private static IEqualityComparer<ChatFlowActionCode> CodeComparer => EqualityComparer<ChatFlowActionCode>.Default;
}