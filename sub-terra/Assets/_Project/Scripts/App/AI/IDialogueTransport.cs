using System.Threading;
using System.Threading.Tasks;

namespace SubTerra.App.AI
{
    public sealed class DialogueTransportResult
    {
        public bool Succeeded { get; }
        public long StatusCode { get; }
        public string ResponseBody { get; }

        public DialogueTransportResult(bool succeeded, long statusCode, string responseBody)
        {
            Succeeded = succeeded;
            StatusCode = statusCode;
            ResponseBody = responseBody ?? string.Empty;
        }
    }

    public interface IDialogueTransport
    {
        Task<DialogueTransportResult> SendAsync(
            string endpoint,
            string requestJson,
            int timeoutMilliseconds,
            CancellationToken cancellationToken);
    }
}
