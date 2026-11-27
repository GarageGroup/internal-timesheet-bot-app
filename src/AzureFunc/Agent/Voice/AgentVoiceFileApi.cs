using System;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentVoiceFileApi(HttpClient httpClient) : IAgentVoiceFileApi, IDisposable
{
    public async ValueTask<byte[]> DownloadAsync(
        string fileUrl,
        int maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                fileUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode is false)
            {
                throw new AgentVoiceFileException();
            }

            if (response.Content.Headers.ContentLength > maxFileSizeBytes)
            {
                throw new AgentVoiceFileTooLargeException();
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var target = new MemoryStream();
            var buffer = new byte[81920];

            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read is 0)
                {
                    return target.ToArray();
                }

                if (target.Length + read > maxFileSizeBytes)
                {
                    throw new AgentVoiceFileTooLargeException();
                }

                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AgentVoiceFileException)
        {
            throw;
        }
        catch (AgentVoiceFileTooLargeException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AgentVoiceFileException();
        }
    }

    public void Dispose()
        =>
        httpClient.Dispose();
}
