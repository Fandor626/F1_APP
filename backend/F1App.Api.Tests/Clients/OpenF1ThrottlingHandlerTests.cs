using System.Net;
using F1App.Api.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1App.Api.Tests.Clients;

public class OpenF1ThrottlingHandlerTests
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
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };
        });

        var handler = new OpenF1ThrottlingHandler(NullLogger<OpenF1ThrottlingHandler>.Instance)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com/") };
        var response = await client.GetAsync("laps?session_key=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond());
    }
}
