using System.Net;
using F1App.Api.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1App.Api.Tests.Clients;

public class ErgastThrottlingHandlerTests
{
    [Fact]
    public async Task SendAsync_RetriesOn429ThenReturnsSuccess()
    {
        var attempts = 0;
        var inner = new StubHandler(() =>
        {
            attempts++;
            return attempts < 2
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var handler = new ErgastThrottlingHandler(NullLogger<ErgastThrottlingHandler>.Instance)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var response = await client.GetAsync("current.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task SendAsync_Returns429AfterExhaustingRetries()
    {
        var attempts = 0;
        var inner = new StubHandler(() =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        });

        var handler = new ErgastThrottlingHandler(NullLogger<ErgastThrottlingHandler>.Instance)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var response = await client.GetAsync("current.json");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(4, attempts); // initial + 3 retries
    }

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond());
    }
}
