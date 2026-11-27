using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GarageGroup.Internal.Timesheet;

#pragma warning disable SKEXP0001, SKEXP0010

internal sealed class AgentVoiceTranscriber(IAudioToTextService audioToTextService) : IAgentVoiceTranscriber
{
    private static readonly string[] SupportedMimeTypes = ["audio/ogg", "audio/mpeg", "audio/mp4", "audio/wav", "audio/webm"];

    public async ValueTask<string> TranscribeAsync(AgentVoiceMessage message, CancellationToken cancellationToken)
    {
        if (SupportedMimeTypes.Contains(message.MimeType, StringComparer.OrdinalIgnoreCase) is false)
        {
            throw new AgentVoiceRecognitionException($"The audio format '{message.MimeType}' is not supported.");
        }

        var settings = new OpenAIAudioToTextExecutionSettings(message.FileName);
        if (string.IsNullOrWhiteSpace(message.Language) is false)
        {
            settings.Language = message.Language;
        }

        string? text;
        try
        {
            var result = await audioToTextService.GetTextContentAsync(
                new AudioContent(message.Audio, message.MimeType), settings, cancellationToken: cancellationToken).ConfigureAwait(false);

            text = result.Text?.Trim();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AgentVoiceRecognitionException("The speech recognition request has failed.", exception);
        }

        return string.IsNullOrEmpty(text) ? throw new AgentVoiceRecognitionException("The recognized text is empty.") : text;
    }
}
