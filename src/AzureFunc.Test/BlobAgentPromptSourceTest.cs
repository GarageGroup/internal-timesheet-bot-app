using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class BlobAgentPromptSourceTest
{
    [Fact]
    public static async Task GetTemplateAsync_BlobExists_ExpectBlobPromptIsCachedUntilExpiration()
    {
        var blob = new FakeBlob { Content = "blob prompt v1" };
        var time = new MutableTime();
        var source = CreateSource(blob, time);

        Assert.Equal("blob prompt v1", await source.GetTemplateAsync(TestContext.Current.CancellationToken));

        blob.Content = "blob prompt v2";
        time.Now = time.Now.AddMinutes(4);

        Assert.Equal("blob prompt v1", await source.GetTemplateAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, blob.DownloadCount);

        // A manual change of the blob is picked up without a deployment after the cache expires
        time.Now = time.Now.AddMinutes(1);

        Assert.Equal("blob prompt v2", await source.GetTemplateAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, blob.DownloadCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n")]
    public static async Task GetTemplateAsync_BlobIsMissingOrEmpty_ExpectPackagedPrompt(string? content)
    {
        var source = CreateSource(new FakeBlob { Content = content }, new MutableTime());

        Assert.Equal("packaged prompt", await source.GetTemplateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task GetTemplateAsync_StorageFailsAfterSuccessfulRead_ExpectLastKnownPromptAndEarlyRetry()
    {
        var blob = new FakeBlob { Content = "blob prompt" };
        var time = new MutableTime();
        var source = CreateSource(blob, time);

        _ = await source.GetTemplateAsync(TestContext.Current.CancellationToken);

        blob.Failure = new InvalidOperationException("storage failure");
        time.Now = time.Now.AddMinutes(5);

        Assert.Equal("blob prompt", await source.GetTemplateAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, blob.DownloadCount);

        // The failure is cached for a minute, then the blob is read again
        time.Now = time.Now.AddSeconds(30);
        _ = await source.GetTemplateAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, blob.DownloadCount);

        blob.Failure = null;
        blob.Content = "restored prompt";
        time.Now = time.Now.AddSeconds(30);

        Assert.Equal("restored prompt", await source.GetTemplateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task GetTemplateAsync_StorageFailsOnFirstRead_ExpectPackagedPrompt()
    {
        var source = CreateSource(new FakeBlob { Failure = new InvalidOperationException("storage failure") }, new MutableTime());

        Assert.Equal("packaged prompt", await source.GetTemplateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task PromptProvider_BlobPromptWithPlaceholders_ExpectPlaceholdersAreReplaced()
    {
        var time = new MutableTime();
        var source = CreateSource(new FakeBlob { Content = "today {{Today}}, language {{Language}}" }, time);
        var provider = new AgentPromptProvider(source, TimeZoneInfo.Utc, time);

        Assert.Equal("today 2026-11-27, language английский (English)", await provider.BuildAsync("en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task PromptProvider_CalendarPlaceholder_ExpectDatesAroundTodayWithWeekdays()
    {
        var time = new MutableTime();
        var provider = new AgentPromptProvider("{{TodayText}}|{{Calendar}}", TimeZoneInfo.Utc, time);

        var russian = await provider.BuildAsync("ru", TestContext.Current.CancellationToken);
        var english = await provider.BuildAsync("en", TestContext.Current.CancellationToken);

        Assert.StartsWith("27 ноября 2026, пятница|2026-10-13 = 13 октября 2026, вторник\n", russian);
        Assert.Contains("\n2026-11-23 = 23 ноября 2026, понедельник\n", russian);
        Assert.Contains("\n2026-11-27 = 27 ноября 2026, пятница\n", russian);
        Assert.EndsWith("\n2026-12-11 = 11 декабря 2026, пятница", russian);
        Assert.Equal(60, russian.Split('\n').Length);

        Assert.StartsWith("27 November 2026, Friday|", english);
        Assert.Contains("\n2026-11-23 = 23 November 2026, Monday\n", english);
    }

    private static BlobAgentPromptSource CreateSource(FakeBlob blob, MutableTime time)
        =>
        new(blob.DownloadAsync, "packaged prompt", TimeSpan.FromMinutes(5), time, NullLogger.Instance);

    private sealed class FakeBlob
    {
        internal string? Content { get; set; }

        internal Exception? Failure { get; set; }

        internal int DownloadCount { get; private set; }

        internal Task<string?> DownloadAsync(CancellationToken cancellationToken)
        {
            DownloadCount++;
            return Failure is null ? Task.FromResult(Content) : Task.FromException<string?>(Failure);
        }
    }

    private sealed class MutableTime : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
            =>
            Now;
    }
}
