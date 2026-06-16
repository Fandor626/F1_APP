using F1App.Api.Clients;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace F1App.Api.Tests.Clients;

public class ErgastClientContractTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_ParsesRaceTableFromCurrentJson()
    {
        _server
            .Given(Request.Create().WithPath("/current.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new
                {
                    RaceTable = new
                    {
                        season = "2026",
                        Races = new[]
                        {
                            new
                            {
                                season = "2026",
                                round = "1",
                                raceName = "Bahrain Grand Prix",
                                Circuit = new
                                {
                                    circuitId = "bahrain",
                                    circuitName = "Bahrain International Circuit",
                                    Location = new { locality = "Sakhir", country = "Bahrain" },
                                },
                                date = "2026-03-08",
                                time = "18:00:00Z",
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var raceTable = await client.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        Assert.Equal("2026", raceTable.Season);
        var race = Assert.Single(raceTable.Races);
        Assert.Equal("Bahrain Grand Prix", race.RaceName);
        Assert.Equal("Bahrain International Circuit", race.Circuit.CircuitName);
        Assert.Equal("Sakhir", race.Circuit.Location.Locality);
        Assert.Equal("Bahrain", race.Circuit.Location.Country);
        Assert.Equal("2026-03-08", race.Date);
        Assert.Equal("18:00:00Z", race.Time);
    }

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_ThrowsOnServerError()
    {
        _server
            .Given(Request.Create().WithPath("/current.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetCurrentSeasonScheduleAsync(CancellationToken.None));
    }

    public void Dispose() => _server.Stop();
}
