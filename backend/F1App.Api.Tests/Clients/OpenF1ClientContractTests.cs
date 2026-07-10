using F1App.Api.Clients;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace F1App.Api.Tests.Clients;

public class OpenF1ClientContractTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task GetLatestLocationsAsync_ParsesDriverCoordinates()
    {
        _server
            .Given(Request.Create().WithPath("/location").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
            {
                new { driver_number = 1, x = -1500.3, y = 823.1, z = 0.0, date = "2024-06-08T12:34:56.123Z", session_key = 9159 },
                new { driver_number = 4, x = -1480.0, y = 810.5, z = 0.0, date = "2024-06-08T12:34:56.200Z", session_key = 9159 },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new OpenF1Client(httpClient);

        var locs = await client.GetLatestLocationsAsync(DateTimeOffset.MinValue, CancellationToken.None);

        Assert.Equal(2, locs.Count);
        Assert.Equal(1, locs[0].DriverNumber);
        Assert.Equal(-1500.3, locs[0].X);
        Assert.Equal(823.1, locs[0].Y);
        Assert.Equal(4, locs[1].DriverNumber);
    }

    [Fact]
    public async Task GetLatestLocationsAsync_ReturnsEmptyArrayWhenNoLocations()
    {
        _server
            .Given(Request.Create().WithPath("/location").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(Array.Empty<object>()));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new OpenF1Client(httpClient);

        var locs = await client.GetLatestLocationsAsync(DateTimeOffset.MinValue, CancellationToken.None);

        Assert.Empty(locs);
    }

    [Fact]
    public async Task GetLatestRaceControlAsync_ParsesMessages()
    {
        _server
            .Given(Request.Create().WithPath("/race_control").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
            {
                new { date = "2024-03-24T04:26:24Z", lap_number = 17, category = "SafetyCar", flag = (string?)null, message = "VIRTUAL SAFETY CAR DEPLOYED" },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new OpenF1Client(httpClient);

        var messages = await client.GetLatestRaceControlAsync(DateTimeOffset.MinValue, CancellationToken.None);

        var msg = Assert.Single(messages);
        Assert.Equal(17, msg.LapNumber);
        Assert.Equal("SafetyCar", msg.Category);
        Assert.Equal("VIRTUAL SAFETY CAR DEPLOYED", msg.Message);
    }

    [Fact]
    public async Task GetLatestPitStopsAsync_ParsesDriverAndLap()
    {
        _server
            .Given(Request.Create().WithPath("/pit").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
            {
                new { date = "2024-09-01T13:11:37Z", driver_number = 27, lap_number = 5 },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new OpenF1Client(httpClient);

        var pits = await client.GetLatestPitStopsAsync(DateTimeOffset.MinValue, CancellationToken.None);

        var pit = Assert.Single(pits);
        Assert.Equal(27, pit.DriverNumber);
        Assert.Equal(5, pit.LapNumber);
    }

    public void Dispose() => _server.Stop();
}
