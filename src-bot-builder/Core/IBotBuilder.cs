using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

public interface IBotBuilder
{
    IBotBuilder Use(Func<IBotContext, CancellationToken, ValueTask<TurnState>> middleware);

    IBot Build();
}