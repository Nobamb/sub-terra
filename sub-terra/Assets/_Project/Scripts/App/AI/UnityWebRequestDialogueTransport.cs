using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace SubTerra.App.AI
{
    /// <summary>인증 비밀이나 제공자 헤더를 넣지 않고 프로젝트의 제한된 HTTPS endpoint만 호출한다.</summary>
    public sealed class UnityWebRequestDialogueTransport : IDialogueTransport
    {
        public async Task<DialogueTransportResult> SendAsync(
            string endpoint,
            string requestJson,
            int timeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            using (var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(requestJson ?? string.Empty));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Math.Max(
                    1,
                    (int)Math.Ceiling(timeoutMilliseconds / 1000d));

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Yield();
                }

                cancellationToken.ThrowIfCancellationRequested();
                return new DialogueTransportResult(
                    request.result == UnityWebRequest.Result.Success,
                    request.responseCode,
                    request.downloadHandler?.text);
            }
        }
    }
}
