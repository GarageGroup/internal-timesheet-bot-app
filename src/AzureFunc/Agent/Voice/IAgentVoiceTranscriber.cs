using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentVoiceMessage(byte[] Audio, string MimeType, string FileName, string Language);

// The voice message could not be recognized: the format is not supported, the speech is empty or the model has failed
internal sealed class AgentVoiceRecognitionException(string message, Exception? innerException = null)
    : Exception(message, innerException);

internal interface IAgentVoiceTranscriber
{
    // Returns the recognized text. Throws AgentVoiceRecognitionException if the message cannot be recognized
    ValueTask<string> TranscribeAsync(AgentVoiceMessage message, CancellationToken cancellationToken);
}
