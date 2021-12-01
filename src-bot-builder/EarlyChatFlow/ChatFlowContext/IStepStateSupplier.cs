using System;

namespace GGroupp.Infra.Bot.Builder;

public interface IStepStateSupplier
{
    public object? StepState { get; }

    public ChatFlowAction<TNext> AwaitAndRetrySameAction<TNext>(Unit _)
        =>
        new(StepState);
}