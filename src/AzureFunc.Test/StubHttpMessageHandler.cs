using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
{
    internal StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : this(request => Task.FromResult(responseFactory(request)))
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        =>
        responseFactory(request);
}
