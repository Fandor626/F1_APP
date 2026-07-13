using System.Net;

namespace F1App.Api.Clients;

// OpenF1 rate-limits burst traffic (e.g. replay building five session
// endpoints in parallel). A shared concurrency gate plus 429 retries keeps
// historical fetches and live polling from tripping the limit.
public sealed class OpenF1ThrottlingHandler(ILogger<OpenF1ThrottlingHandler> logger) : DelegatingHandler
{
    private const int MaxConcurrentRequests = 1;

    private static readonly SemaphoreSlim ConcurrencyGate = new(MaxConcurrentRequests, MaxConcurrentRequests);
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
    ];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await ConcurrencyGate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= RetryDelays.Length)
                    return response;

                response.Dispose();
                var delay = RetryDelays[attempt];
                logger.LogWarning(
                    "OpenF1 rate-limited (429) on {Method} {Uri} — retry {Attempt}/{MaxAttempts} in {Delay}",
                    request.Method,
                    request.RequestUri,
                    attempt + 1,
                    RetryDelays.Length,
                    delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
        finally
        {
            ConcurrencyGate.Release();
        }
    }
}
