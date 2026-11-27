using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

// Reads the system prompt from a blob, so it can be changed without deploying the bot.
// The prompt packaged with the application is used when the blob does not exist, is empty or cannot be read.
// The download delegate returns null if the blob does not exist
internal sealed class BlobAgentPromptSource(
    Func<CancellationToken, Task<string?>> download,
    string packagedTemplate,
    TimeSpan cacheDuration,
    TimeProvider timeProvider,
    ILogger logger)
    : IAgentPromptSource
{
    // A failed read is retried sooner than a successful one is refreshed
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromMinutes(1);

    private volatile CachedTemplate? cached;

    public async ValueTask<string> GetTemplateAsync(CancellationToken cancellationToken)
    {
        var current = cached;
        var now = timeProvider.GetUtcNow();

        if (current is not null && current.ExpiresAt > now)
        {
            return current.Value;
        }

        try
        {
            var blobTemplate = await download.Invoke(cancellationToken).ConfigureAwait(false);
            var template = string.IsNullOrWhiteSpace(blobTemplate) ? packagedTemplate : blobTemplate;

            cached = new(template, now.Add(cacheDuration));
            return template;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The last known prompt keeps working while the storage is unavailable
            var template = current?.Value ?? packagedTemplate;
            logger.LogWarning(exception, "Failed to read the agent prompt from Blob Storage. The previous prompt is used");

            var retryDelay = cacheDuration < FailureRetryDelay ? cacheDuration : FailureRetryDelay;
            cached = new(template, now.Add(retryDelay));

            return template;
        }
    }

    private sealed record class CachedTemplate(string Value, DateTimeOffset ExpiresAt);
}
