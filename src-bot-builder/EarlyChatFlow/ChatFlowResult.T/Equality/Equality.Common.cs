using System;
using System.Collections.Generic;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowResult<T>
{
    private static Type EqualityContract => typeof(ChatFlowResult<T>);

    private static IEqualityComparer<T> FlowStateComparer => EqualityComparer<T>.Default;

    private static IEqualityComparer<ChatFlowResultCode> CodeComparer => EqualityComparer<ChatFlowResultCode>.Default;
}