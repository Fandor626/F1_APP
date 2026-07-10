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
    public async Task GetConstructorStandingsByRoundAsync_ParsesTopLevelStandingsList()
    {
        _server
            .Given(Request.Create().WithPath("/current/5/constructorStandings.json").UsingGet())
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
                                        position = "2",
                                        points = "80",
                                        wins = "1",
                                        Constructor = new { constructorId = "ferrari", name = "Ferrari" },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var standings = await client.GetConstructorStandingsByRoundAsync(5, CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal("Ferrari", standing.Constructor.Name);
        Assert.Equal("2", standing.Position);
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

    [Fact]
    public async Task GetLastRaceResultsAsync_ParsesRaceNameAndResultsFromCurrentLastJson()
    {
        _server
            .Given(Request.Create().WithPath("/current/last/results.json").UsingGet())
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
                                raceName = "Canadian Grand Prix",
                                date = "2026-06-08",
                                Results = new[]
                                {
                                    new
                                    {
                                        position = "1",
                                        number = "4",
                                        Driver = new { driverId = "norris", givenName = "Lando", familyName = "Norris", code = "NOR" },
                                        Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                        Time = new { time = "1:32:13.576" },
                                        status = "Finished",
                                    },
                                    new
                                    {
                                        position = "2",
                                        number = "81",
                                        Driver = new { driverId = "piastri", givenName = "Oscar", familyName = "Piastri", code = "PIA" },
                                        Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                        Time = new { time = "+5.014" },
                                        status = "Finished",
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var result = await client.GetLastRaceResultsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Canadian Grand Prix", result.RaceName);
        Assert.Equal(2, result.Results.Count);
        Assert.Equal("1", result.Results[0].Position);
        Assert.Equal("NOR", result.Results[0].Driver.Code);
        Assert.Equal("McLaren", result.Results[0].Constructor.Name);
        Assert.Equal("+5.014", result.Results[1].Time?.Time);
    }

    [Fact]
    public async Task GetLastRaceResultsAsync_ReturnsNullWhenNoCompletedRaceThisSeason()
    {
        _server
            .Given(Request.Create().WithPath("/current/last/results.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { RaceTable = new { Races = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var result = await client.GetLastRaceResultsAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRaceResultsByRoundAsync_ParsesResultsWithPoints()
    {
        _server
            .Given(Request.Create().WithPath("/current/1/results.json").UsingGet())
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
                                raceName = "Bahrain Grand Prix",
                                round = "1",
                                date = "2026-03-08",
                                Results = new[]
                                {
                                    new
                                    {
                                        position = "1",
                                        number = "4",
                                        Driver = new { driverId = "norris", givenName = "Lando", familyName = "Norris", code = "NOR" },
                                        Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                        Time = new { time = "1:32:13.576" },
                                        status = "Finished",
                                        points = "25",
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var result = await client.GetRaceResultsByRoundAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Bahrain Grand Prix", result.RaceName);
        var single = Assert.Single(result.Results);
        Assert.Equal("25", single.Points);
        Assert.Equal("1", single.Position);
    }

    [Fact]
    public async Task GetRaceResultsByRoundAsync_ReturnsNullWhenRoundNotYetRun()
    {
        _server
            .Given(Request.Create().WithPath("/current/20/results.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { RaceTable = new { Races = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var result = await client.GetRaceResultsByRoundAsync(20, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCircuitPitStopsAsync_ResolvesRoundThenReturnsPitStops()
    {
        _server
            .Given(Request.Create().WithPath("/2025/circuits/monza/results/1.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { RaceTable = new { Races = new[] { new { round = "16", Results = Array.Empty<object>() } } } },
            }));
        _server
            .Given(Request.Create().WithPath("/2025/16/pitstops.json").UsingGet())
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
                                PitStops = new[]
                                {
                                    new { driverId = "norris", lap = "18", stop = "1" },
                                    new { driverId = "piastri", lap = "20", stop = "1" },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var pitStops = await client.GetCircuitPitStopsAsync(2025, "monza", CancellationToken.None);

        Assert.Equal(2, pitStops.Count);
        Assert.Equal("norris", pitStops[0].DriverId);
        Assert.Equal("18", pitStops[0].Lap);
    }

    [Fact]
    public async Task GetCircuitPitStopsAsync_ReturnsEmptyWhenNoRaceAtCircuitThatSeason()
    {
        _server
            .Given(Request.Create().WithPath("/2025/circuits/las_vegas/results/1.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { RaceTable = new { Races = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var pitStops = await client.GetCircuitPitStopsAsync(2025, "las_vegas", CancellationToken.None);

        Assert.Empty(pitStops);
    }

    [Fact]
    public async Task GetCircuitInfoAsync_ParsesCircuitNameAndLocation()
    {
        _server
            .Given(Request.Create().WithPath("/circuits/monza.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new
                {
                    CircuitTable = new
                    {
                        Circuits = new[]
                        {
                            new
                            {
                                circuitId = "monza",
                                circuitName = "Autodromo Nazionale di Monza",
                                Location = new { locality = "Monza", country = "Italy" },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var circuit = await client.GetCircuitInfoAsync("monza", CancellationToken.None);

        Assert.NotNull(circuit);
        Assert.Equal("Autodromo Nazionale di Monza", circuit!.CircuitName);
        Assert.Equal("Monza", circuit.Location.Locality);
        Assert.Equal("Italy", circuit.Location.Country);
    }

    [Fact]
    public async Task GetCircuitInfoAsync_ReturnsNullWhenCircuitIdUnknown()
    {
        _server
            .Given(Request.Create().WithPath("/circuits/not_a_circuit.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { CircuitTable = new { Circuits = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var circuit = await client.GetCircuitInfoAsync("not_a_circuit", CancellationToken.None);

        Assert.Null(circuit);
    }

    [Fact]
    public async Task GetAllCircuitResultsAsync_ParsesMultipleSeasonsOfRaces()
    {
        _server
            .Given(Request.Create().WithPath("/circuits/monza/results.json").UsingGet())
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
                                season = "2023",
                                raceName = "Italian Grand Prix",
                                round = "15",
                                Results = new[]
                                {
                                    new
                                    {
                                        position = "1",
                                        Driver = new { driverId = "verstappen", givenName = "Max", familyName = "Verstappen" },
                                        Constructor = new { constructorId = "red_bull", name = "Red Bull Racing" },
                                        FastestLap = new { rank = "1", Time = new { time = "1:47.930" } },
                                    },
                                },
                            },
                            new
                            {
                                season = "2024",
                                raceName = "Italian Grand Prix",
                                round = "16",
                                Results = new[]
                                {
                                    new
                                    {
                                        position = "1",
                                        Driver = new { driverId = "norris", givenName = "Lando", familyName = "Norris" },
                                        Constructor = new { constructorId = "mclaren", name = "McLaren" },
                                        FastestLap = new { rank = "1", Time = new { time = "1:26.720" } },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var races = await client.GetAllCircuitResultsAsync("monza", CancellationToken.None);

        Assert.Equal(2, races.Count);
        Assert.Equal("2023", races[0].Season);
        Assert.Equal("2024", races[1].Season);
        Assert.Equal("1", races[1].Results[0].FastestLap?.Rank);
        Assert.Equal("1:26.720", races[1].Results[0].FastestLap?.Time?.Time);
    }

    [Fact]
    public async Task GetDriverInfoAsync_ParsesDriverBio()
    {
        _server
            .Given(Request.Create().WithPath("/drivers/max_verstappen.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new
                {
                    DriverTable = new
                    {
                        Drivers = new[]
                        {
                            new { driverId = "max_verstappen", givenName = "Max", familyName = "Verstappen", nationality = "Dutch" },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var driver = await client.GetDriverInfoAsync("max_verstappen", CancellationToken.None);

        Assert.NotNull(driver);
        Assert.Equal("Max", driver!.GivenName);
        Assert.Equal("Verstappen", driver.FamilyName);
        Assert.Equal("Dutch", driver.Nationality);
    }

    [Fact]
    public async Task GetDriverInfoAsync_ReturnsNullWhenDriverIdUnknown()
    {
        _server
            .Given(Request.Create().WithPath("/drivers/not_a_driver.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { DriverTable = new { Drivers = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var driver = await client.GetDriverInfoAsync("not_a_driver", CancellationToken.None);

        Assert.Null(driver);
    }

    [Fact]
    public async Task GetAllDriverResultsAsync_ParsesMultipleSeasonsOfRaces()
    {
        _server
            .Given(Request.Create().WithPath("/drivers/max_verstappen/results.json").UsingGet())
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
                                season = "2015",
                                round = "1",
                                raceName = "Australian Grand Prix",
                                Results = new[]
                                {
                                    new
                                    {
                                        position = "13",
                                        grid = "11",
                                        status = "Engine",
                                        points = "0",
                                        Driver = new { driverId = "max_verstappen", givenName = "Max", familyName = "Verstappen" },
                                        Constructor = new { constructorId = "toro_rosso", name = "Toro Rosso" },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var races = await client.GetAllDriverResultsAsync("max_verstappen", CancellationToken.None);

        var race = Assert.Single(races);
        Assert.Equal("2015", race.Season);
        Assert.Equal("11", race.Results[0].Grid);
        Assert.Equal("Engine", race.Results[0].Status);
    }

    [Fact]
    public async Task GetSeasonChampionAsync_ParsesChampionFromDriverStandingsPositionOne()
    {
        _server
            .Given(Request.Create().WithPath("/2023/driverStandings/1.json").UsingGet())
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
                                        points = "575",
                                        wins = "19",
                                        Driver = new { driverId = "max_verstappen", givenName = "Max", familyName = "Verstappen" },
                                        Constructors = new[] { new { constructorId = "red_bull", name = "Red Bull" } },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var champion = await client.GetSeasonChampionAsync(2023, CancellationToken.None);

        Assert.NotNull(champion);
        Assert.Equal("max_verstappen", champion!.Driver.DriverId);
    }

    [Fact]
    public async Task GetSeasonChampionAsync_ReturnsNullWhenNoStandingsForThatSeason()
    {
        _server
            .Given(Request.Create().WithPath("/1930/driverStandings/1.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                MRData = new { StandingsTable = new { StandingsLists = Array.Empty<object>() } },
            }));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        var client = new ErgastClient(httpClient);

        var champion = await client.GetSeasonChampionAsync(1930, CancellationToken.None);

        Assert.Null(champion);
    }

    public void Dispose() => _server.Stop();
}
