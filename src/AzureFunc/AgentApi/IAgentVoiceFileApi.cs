using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentVoiceFileApi
{
    ValueTask<byte[]> DownloadAsync(string fileUrl, int maxFileSizeBytes, CancellationToken cancellationToken);
}
