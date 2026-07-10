using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class HeadToHeadServiceTests
{
    private static ErgastDriverDto DriverInfo(string driverId, string given, string family) =>
        new(driverId, given, family);

    private static ErgastResultDto RaceResult(string driverId, string constructorName, string? position, string status = "Finished", string points = "0", string? fastestLapRank = null) =>
        new(
            new ErgastDriverDto(driverId, "Given", driverId),
            new ErgastConstructorDto("cid", constructorName),
            null,
            position,
            null,
            status,
            points,
            null,
            fastestLapRank is null ? null : new ErgastFastestLapDto(fastestLapRank, new ErgastResultTimeDto("1:30.000")));

    private static ErgastRaceResultRaceDto Race(ErgastResultDto result) =>
        new("Race", "1", "2026-01-01", [result], "2023");

    private static ErgastQualifyingResultDto QualiResult(string driverId, string position) =>
        new(position, new ErgastDriverDto(driverId, "Given", driverId), new ErgastConstructorDto("cid", "Team"));

    private static ErgastQualifyingRaceDto QualiRace(ErgastQualifyingResultDto result) =>
        new(new ErgastCircuitDto("cid", "Circuit", new ErgastLocationDto("City", "Country")), [result]);

    [Fact]
    public async Task GetAllDriversAsync_ReturnsSortedDriverOptions()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetAllDriversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverInfo("verstappen", "Max", "Verstappen"), DriverInfo("hamilton", "Lewis", "Hamilton")]);

        var service = new HeadToHeadService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var drivers = await service.GetAllDriversAsync(CancellationToken.None);

        Assert.Equal(2, drivers.Count);
        Assert.Equal("Lewis Hamilton", drivers[0].FullName); // alphabetical
        Assert.Equal("Max Verstappen", drivers[1].FullName);
    }

    [Fact]
    public async Task CompareAsync_ReturnsNullWhenEitherDriverNotFound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverInfo("a", "Driver", "A"));
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastDriverDto?)null);
        ergastClient
            .Setup(c => c.GetFilteredDriverResultsAsync("a", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ergastClient
            .Setup(c => c.GetDriverQualifyingHistoryAsync("a", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new HeadToHeadService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var comparison = await service.CompareAsync("a", "unknown", null, null, CancellationToken.None);

        Assert.Null(comparison);
    }

    [Fact]
    public async Task CompareAsync_ComputesStatsForBothDrivers()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient.Setup(c => c.GetDriverInfoAsync("a", It.IsAny<CancellationToken>())).ReturnsAsync(DriverInfo("a", "Driver", "A"));
        ergastClient.Setup(c => c.GetDriverInfoAsync("b", It.IsAny<CancellationToken>())).ReturnsAsync(DriverInfo("b", "Driver", "B"));

        ergastClient
            .Setup(c => c.GetFilteredDriverResultsAsync("a", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Race(RaceResult("a", "Team", "1", "Finished", "25", "1"))]); // win + fastest lap
        ergastClient
            .Setup(c => c.GetDriverQualifyingHistoryAsync("a", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([QualiRace(QualiResult("a", "1"))]);

        ergastClient
            .Setup(c => c.GetFilteredDriverResultsAsync("b", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Race(RaceResult("b", "Team", null, "Accident", "0"))]); // DNF
        ergastClient
            .Setup(c => c.GetDriverQualifyingHistoryAsync("b", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([QualiRace(QualiResult("b", "20"))]);

        var service = new HeadToHeadService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var comparison = await service.CompareAsync("a", "b", null, null, CancellationToken.None);

        Assert.NotNull(comparison);
        Assert.Equal(1, comparison!.DriverA.Wins);
        Assert.Equal(1, comparison.DriverA.FastestLaps);
        Assert.Equal(0, comparison.DriverA.DnfCount);
        Assert.Equal(25, comparison.DriverA.PointsScored);
        Assert.Equal(1, comparison.DriverA.QualifyingAveragePosition);
        Assert.Equal(1, comparison.DriverA.RaceFinishAveragePosition);

        Assert.Equal(0, comparison.DriverB.Wins);
        Assert.Equal(1, comparison.DriverB.DnfCount);
        Assert.Equal(20, comparison.DriverB.QualifyingAveragePosition);
        Assert.Null(comparison.DriverB.RaceFinishAveragePosition); // no parseable finish position
    }

    [Fact]
    public async Task CompareAsync_PassesSeasonAndCircuitFiltersThroughToErgastClient()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient.Setup(c => c.GetDriverInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => DriverInfo(id, "Given", id));
        ergastClient.Setup(c => c.GetFilteredDriverResultsAsync(It.IsAny<string>(), 2023, "monza", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        ergastClient.Setup(c => c.GetDriverQualifyingHistoryAsync(It.IsAny<string>(), 2023, "monza", It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var service = new HeadToHeadService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        await service.CompareAsync("a", "b", 2023, "monza", CancellationToken.None);

        ergastClient.Verify(c => c.GetFilteredDriverResultsAsync("a", 2023, "monza", It.IsAny<CancellationToken>()), Times.Once);
        ergastClient.Verify(c => c.GetFilteredDriverResultsAsync("b", 2023, "monza", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompareAsync_ReturnsNullAveragesWhenNoQualifyingOrRaceData()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient.Setup(c => c.GetDriverInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => DriverInfo(id, "Given", id));
        ergastClient.Setup(c => c.GetFilteredDriverResultsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        ergastClient.Setup(c => c.GetDriverQualifyingHistoryAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var service = new HeadToHeadService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var comparison = await service.CompareAsync("a", "b", 1930, "nonexistent", CancellationToken.None);

        Assert.NotNull(comparison);
        Assert.Null(comparison!.DriverA.QualifyingAveragePosition);
        Assert.Null(comparison.DriverA.RaceFinishAveragePosition);
        Assert.Equal(0, comparison.DriverA.RacesCompared);
    }
}
