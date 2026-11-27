using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet.Test;

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        // Confirmed actions are executed in parallel, so requests may arrive at the same time
        lock (Requests)
        {
            Requests.Add(
                new(
                    Method: request.Method.Method,
                    Uri: request.RequestUri!,
                    Authorization: request.Headers.Authorization?.ToString(),
                    SubscriptionKey: request.Headers.TryGetValues("Ocp-Apim-Subscription-Key", out var values) ? string.Join(',', values) : null,
                    Body: body));
        }

        return respond(request);
    }

    internal static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    internal sealed record class RecordedRequest(string Method, Uri Uri, string? Authorization, string? SubscriptionKey, string Body);
}
