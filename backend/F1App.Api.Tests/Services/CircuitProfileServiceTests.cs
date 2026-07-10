using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class CircuitProfileServiceTests
{
    private static ErgastCircuitDto CircuitInfo(string circuitId = "monza", string name = "Autodromo Nazionale di Monza") =>
        new(circuitId, name, new ErgastLocationDto("Monza", "Italy"));

    private static ErgastResultDto Result(string driverId, string constructorName, string position, string? fastestLapRank = null, string? fastestLapTime = null) =>
        new(
            new ErgastDriverDto(driverId, "Given", driverId),
            new ErgastConstructorDto("cid", constructorName),
            null,
            position,
            null,
            "Finished",
            "25",
            "1",
            fastestLapRank is null ? null : new ErgastFastestLapDto(fastestLapRank, new ErgastResultTimeDto(fastestLapTime!)));

    private static ErgastRaceResultRaceDto Race(string season, params ErgastResultDto[] results) =>
        new("Italian Grand Prix", "1", "2026-01-01", results, season);

    [Fact]
    public async Task GetCircuitProfileAsync_ReturnsNullWhenCircuitInfoNotFound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCircuitInfoAsync("not_a_circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastCircuitDto?)null);

        var service = new CircuitProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetCircuitProfileAsync("not_a_circuit", CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task GetCircuitProfileAsync_ComputesFirstSeasonWinnersAndLapRecord()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCircuitInfoAsync("monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CircuitInfo());
        ergastClient
            .Setup(c => c.GetAllCircuitResultsAsync("monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Race("2023", Result("verstappen", "Red Bull Racing", "1", "1", "1:47.930")),
                Race("2024", Result("norris", "McLaren", "1", "1", "1:26.720")), // faster -> should win the record
            ]);

        var service = new CircuitProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetCircuitProfileAsync("monza", CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(2023, profile!.FirstF1Season);
        Assert.Equal(2, profile.PastWinners.Count);
        Assert.Equal(2024, profile.PastWinners[0].Season); // most recent first
        Assert.NotNull(profile.LapRecord);
        Assert.Equal("Given Norris", profile.LapRecord!.DriverName);
        Assert.Equal("1:26.720", profile.LapRecord.Time);
        Assert.Equal(2024, profile.LapRecord.Season);
        Assert.NotNull(profile.Stats); // monza is in the static facts table
    }

    [Fact]
    public async Task GetCircuitProfileAsync_UnknownCircuitStats_ReturnsNullStats()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCircuitInfoAsync("some_obscure_circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CircuitInfo("some_obscure_circuit", "Obscure Circuit"));
        ergastClient
            .Setup(c => c.GetAllCircuitResultsAsync("some_obscure_circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Race("1970", Result("driver", "Team", "1"))]);

        var service = new CircuitProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetCircuitProfileAsync("some_obscure_circuit", CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Null(profile!.Stats);
        Assert.Null(profile.LapRecord); // no FastestLap data at all in this fixture
    }

    [Fact]
    public async Task GetCircuitProfileAsync_CachesResultAndDoesNotCallErgastTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCircuitInfoAsync("monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CircuitInfo());
        ergastClient
            .Setup(c => c.GetAllCircuitResultsAsync("monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Race("2024", Result("norris", "McLaren", "1"))]);

        var service = new CircuitProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        await service.GetCircuitProfileAsync("monza", CancellationToken.None);
        await service.GetCircuitProfileAsync("monza", CancellationToken.None);

        ergastClient.Verify(c => c.GetCircuitInfoAsync("monza", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void FindLapRecord_IgnoresNonRankOneFastestLaps()
    {
        var races = new[]
        {
            Race("2024",
                Result("norris", "McLaren", "1", "2", "1:20.000"), // rank 2, not the fastest of its own race
                Result("verstappen", "Red Bull Racing", "2", "1", "1:25.000")),
        };

        var record = CircuitProfileService.FindLapRecord(races);

        Assert.NotNull(record);
        Assert.Equal("Given Verstappen", record!.DriverName);
        Assert.Equal("1:25.000", record.Time);
    }

    [Fact]
    public void FindLapRecord_ReturnsNullWhenNoFastestLapData()
    {
        var races = new[] { Race("1970", Result("driver", "Team", "1")) };

        Assert.Null(CircuitProfileService.FindLapRecord(races));
    }

    [Theory]
    [InlineData("1:27.573", 87.573)]
    [InlineData("1:20.170", 80.170)]
    public void ParseLapTimeSeconds_ParsesMinutesSecondsMillis(string time, double expectedSeconds)
    {
        Assert.Equal(expectedSeconds, CircuitProfileService.ParseLapTimeSeconds(time), precision: 3);
    }
}
