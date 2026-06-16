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

    [Fact]
    public async Task GetCurrentDriverStandingsAsync_ParsesTopLevelStandingsList()
    {
        _server
            .Given(Request.Create().WithPath("/current/driverStandings.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new
                {
                    StandingsTable = new
                    {
                        StandingsLists = new[]
                        {
                            new
                            {
                                DriverStandings = new[]
                                {
                                    new
                                    {
                                        position = "1",
                                        points = "156",
                                        wins = "5",
                                        Driver = new { driverId = "antonelli", givenName = "Andrea Kimi", familyName = "Antonelli" },
                                        Constructors = new[] { new { constructorId = "mercedes", name = "Mercedes" } },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var standings = await client.GetCurrentDriverStandingsAsync(CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal("Antonelli", standing.Driver.FamilyName);
        Assert.Equal("Mercedes", standing.Constructors[0].Name);
        Assert.Equal("156", standing.Points);
    }

    [Fact]
    public async Task GetCurrentConstructorStandingsAsync_ParsesTopLevelStandingsList()
    {
        _server
            .Given(Request.Create().WithPath("/current/constructorStandings.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new
                {
                    StandingsTable = new
                    {
                        StandingsLists = new[]
                        {
                            new
                            {
                                ConstructorStandings = new[]
                                {
                                    new
                                    {
                                        position = "1",
                                        points = "262",
                                        wins = "6",
                                        Constructor = new { constructorId = "mercedes", name = "Mercedes" },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var standings = await client.GetCurrentConstructorStandingsAsync(CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal("Mercedes", standing.Constructor.Name);
        Assert.Equal("262", standing.Points);
    }

    [Fact]
    public async Task GetCircuitResultsAsync_ParsesWinnerFromFirstRace()
    {
        _server
            .Given(Request.Create().WithPath("/2025/circuits/bahrain/results/1.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new
                {
                    RaceTable = new
                    {
                        Races = new[]
                        {
                            new
                            {
                                Results = new[]
                                {
                                    new
                                    {
                                        Driver = new { driverId = "piastri", givenName = "Oscar", familyName = "Piastri" },
                                        Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                        Time = new { millis = "5739435", time = "1:35:39.435" },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var results = await client.GetCircuitResultsAsync(2025, "bahrain", CancellationToken.None);

        var winner = Assert.Single(results);
        Assert.Equal("Oscar", winner.Driver.GivenName);
        Assert.Equal("Piastri", winner.Driver.FamilyName);
        Assert.Equal("McLaren", winner.Constructor.Name);
        Assert.Equal("1:35:39.435", winner.Time?.Time);
    }

    [Fact]
    public async Task GetCircuitResultsAsync_ReturnsEmptyListWhenNoRaceAtCircuitThatSeason()
    {
        _server
            .Given(Request.Create().WithPath("/2025/circuits/las_vegas/results/1.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { RaceTable = new { Races = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var results = await client.GetCircuitResultsAsync(2025, "las_vegas", CancellationToken.None);

        Assert.Empty(results);
    }

    public void Dispose() => _server.Stop();
}
