using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

#pragma warning disable SKEXP0001, SKEXP0010

public static class AgentVoiceTranscriberTest
{
    [Fact]
    public static async Task TranscribeAsync_SupportedAudio_ExpectTrimmedTextAndLanguageAndFileNameArePassed()
    {
        var service = new FakeAudioToTextService("  покажи списания за сегодня \n");
        var transcriber = new AgentVoiceTranscriber(service);

        var actual = await transcriber.TranscribeAsync(
            new([1, 2, 3], "audio/ogg", "voice.oga", "ru"), TestContext.Current.CancellationToken);

        Assert.Equal("покажи списания за сегодня", actual);

        var call = Assert.Single(service.Calls);
        Assert.Equal("audio/ogg", call.Content.MimeType);
        Assert.Equal([1, 2, 3], call.Content.Data!.Value.ToArray());
        Assert.Equal("voice.oga", call.Settings.Filename);
        Assert.Equal("ru", call.Settings.Language);
    }

    [Theory]
    [InlineData("audio/flac", "text")]
    [InlineData("audio/ogg", "   ")]
    public static async Task TranscribeAsync_UnsupportedFormatOrEmptyText_ExpectRecognitionException(string mimeType, string text)
    {
        var transcriber = new AgentVoiceTranscriber(new FakeAudioToTextService(text));

        _ = await Assert.ThrowsAsync<AgentVoiceRecognitionException>(
            async () => await transcriber.TranscribeAsync(new([1], mimeType, "voice.oga", "ru"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task TranscribeAsync_ModelFails_ExpectRecognitionExceptionWithInnerException()
    {
        var failure = new InvalidOperationException("model failure");
        var transcriber = new AgentVoiceTranscriber(new FakeAudioToTextService(failure));

        var exception = await Assert.ThrowsAsync<AgentVoiceRecognitionException>(
            async () => await transcriber.TranscribeAsync(new([1], "audio/ogg", "voice.oga", "ru"), TestContext.Current.CancellationToken));

        Assert.Same(failure, exception.InnerException);
    }

    private sealed class FakeAudioToTextService : IAudioToTextService
    {
        private readonly string? text;

        private readonly Exception? failure;

        internal FakeAudioToTextService(string text)
            =>
            this.text = text;

        internal FakeAudioToTextService(Exception failure)
            =>
            this.failure = failure;

        internal List<(AudioContent Content, OpenAIAudioToTextExecutionSettings Settings)> Calls { get; } = [];

        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
            AudioContent content,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((content, (OpenAIAudioToTextExecutionSettings)executionSettings!));

            return failure is null
                ? Task.FromResult<IReadOnlyList<TextContent>>([new(text)])
                : Task.FromException<IReadOnlyList<TextContent>>(failure);
        }
    }
}
