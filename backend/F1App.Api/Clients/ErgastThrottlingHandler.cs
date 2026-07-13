using System.Net;

namespace F1App.Api.Clients;

// Jolpica/Ergast rate-limits aggressively when many requests fire at once.
// A shared concurrency gate plus 429 retries keeps cold-cache bursts (e.g.
// fetching ~24 circuit profiles on first /api/races load) from failing the
// whole request.
public sealed class ErgastThrottlingHandler(ILogger<ErgastThrottlingHandler> logger) : DelegatingHandler
{
    private const int MaxConcurrentRequests = 2;

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
                    "Ergast rate-limited (429) on {Method} {Uri} — retry {Attempt}/{MaxAttempts} in {Delay}",
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
