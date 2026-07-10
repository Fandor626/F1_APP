using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class SeasonWrappedServiceTests
{
    private static RaceWeekendSummary Race(int round, string raceName) =>
        new(2026, round, raceName, "circuit-id", "Circuit", "City", "Country", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static ErgastResultDto Result(string driverId, string constructorName, string? grid, string? pos, string status, string points) =>
        new(new ErgastDriverDto(driverId, "Given", driverId), new ErgastConstructorDto("cid", constructorName), null, pos, null, status, points, grid);

    private static ErgastRaceResultRaceDto RaceResult(string raceName, params ErgastResultDto[] results) =>
        new(raceName, "1", "2026-01-01", results);

    private static ErgastConstructorStandingDto ConstructorStanding(string position, string name) =>
        new(position, "0", "0", new ErgastConstructorDto("cid", name));

    private static (int Round, string RaceName, ErgastRaceResultRaceDto Result) Round(int round, ErgastRaceResultRaceDto result) =>
        (round, result.RaceName, result);

    // ----- GetSeasonWrappedAsync orchestration -----

    [Fact]
    public async Task GetSeasonWrappedAsync_ReturnsNullAndShortCircuitsWhenSeasonInProgress()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastRaceResultRaceDto?)null);

        var service = new SeasonWrappedService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix"), Race(2, "Saudi Arabian Grand Prix") };

        var wrapped = await service.GetSeasonWrappedAsync(schedule, CancellationToken.None);

        Assert.Null(wrapped);
        ergastClient.Verify(c => c.GetRaceResultsByRoundAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        ergastClient.Verify(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSeasonWrappedAsync_CachesNullResultAndDoesNotRecheckFinalRound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastRaceResultRaceDto?)null);

        var service = new SeasonWrappedService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix") };

        await service.GetSeasonWrappedAsync(schedule, CancellationToken.None);
        await service.GetSeasonWrappedAsync(schedule, CancellationToken.None);

        ergastClient.Verify(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSeasonWrappedAsync_ComputesAllFiveAwardsFromASmallCompletedSeason()
    {
        var ergastClient = new Mock<IErgastClient>();

        var round1 = RaceResult(
            "Bahrain Grand Prix",
            Result("a", "McLaren", "1", "1", "Finished", "25"),
            Result("b", "RedBull", "2", "2", "Finished", "18"),
            Result("c", "Ferrari", "3", null, "Accident", "0"));

        var round2 = RaceResult(
            "Saudi Arabian Grand Prix",
            Result("a", "McLaren", "5", "1", "Finished", "25"),
            Result("b", "RedBull", "1", "5", "Finished", "0"),
            Result("c", "Ferrari", "2", "2", "Finished", "18"));

        var round3 = RaceResult(
            "Australian Grand Prix",
            Result("a", "McLaren", "3", "3", "Finished", "15"),
            Result("b", "RedBull", "1", "1", "Finished", "25"),
            Result("c", "Ferrari", "2", "4", "+1 Lap", "12"));

        ergastClient.Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(round1);
        ergastClient.Setup(c => c.GetRaceResultsByRoundAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(round2);
        ergastClient.Setup(c => c.GetRaceResultsByRoundAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(round3);

        ergastClient
            .Setup(c => c.GetConstructorStandingsByRoundAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("3", "McLaren"), ConstructorStanding("1", "RedBull"), ConstructorStanding("2", "Ferrari")]);
        ergastClient
            .Setup(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("1", "McLaren"), ConstructorStanding("2", "RedBull"), ConstructorStanding("3", "Ferrari")]);

        var service = new SeasonWrappedService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix"), Race(2, "Saudi Arabian Grand Prix"), Race(3, "Australian Grand Prix") };

        var wrapped = await service.GetSeasonWrappedAsync(schedule, CancellationToken.None);

        Assert.NotNull(wrapped);

        Assert.Equal("Saudi Arabian Grand Prix", wrapped!.MostDramaticRace.RaceName);
        Assert.Equal(8, wrapped.MostDramaticRace.TotalPositionSwing);

        Assert.Equal("c", wrapped.MostDnfs.DriverId);
        Assert.Equal(1, wrapped.MostDnfs.Value);

        Assert.Equal("b", wrapped.BiggestPointsComeback.DriverId);
        Assert.Equal(10, wrapped.BiggestPointsComeback.Value);

        Assert.Equal("a", wrapped.MostPositionsGainedInARace.DriverId);
        Assert.Equal("Saudi Arabian Grand Prix", wrapped.MostPositionsGainedInARace.RaceName);
        Assert.Equal(4, wrapped.MostPositionsGainedInARace.PositionsGained);

        Assert.Equal("McLaren", wrapped.MostImprovedConstructor.ConstructorName);
        Assert.Equal(3, wrapped.MostImprovedConstructor.EarlySeasonPosition);
        Assert.Equal(1, wrapped.MostImprovedConstructor.FinalPosition);
        Assert.Equal(2, wrapped.MostImprovedConstructor.PositionsImproved);
    }

    // ----- FindMostDramaticRace -----

    [Fact]
    public void FindMostDramaticRace_PicksRaceWithLargestTotalPositionSwing()
    {
        var quiet = Round(1, RaceResult("Quiet GP", Result("a", "T", "1", "1", "Finished", "25")));
        var wild = Round(2, RaceResult("Wild GP", Result("a", "T", "5", "1", "Finished", "25"), Result("b", "T2", "1", "5", "Finished", "0")));

        var result = SeasonWrappedService.FindMostDramaticRace([quiet, wild]);

        Assert.NotNull(result);
        Assert.Equal("Wild GP", result!.RaceName);
        Assert.Equal(8, result.TotalPositionSwing);
    }

    [Fact]
    public void FindMostDramaticRace_SkipsResultsWithUnparseableGridOrPosition()
    {
        var race = Round(1, RaceResult("GP", Result("a", "T", null, "1", "Accident", "0")));

        var result = SeasonWrappedService.FindMostDramaticRace([race]);

        Assert.NotNull(result);
        Assert.Equal(0, result!.TotalPositionSwing);
    }

    [Fact]
    public void FindMostDramaticRace_ReturnsNullWhenNoRounds()
    {
        Assert.Null(SeasonWrappedService.FindMostDramaticRace([]));
    }

    // ----- FindMostDnfs -----

    [Fact]
    public void FindMostDnfs_CountsNonFinishedNonLappedStatusesOnly()
    {
        var race = Round(1, RaceResult(
            "GP",
            Result("a", "T", "1", "1", "Finished", "25"),
            Result("b", "T2", "2", "2", "+1 Lap", "18"),
            Result("c", "T3", "3", null, "Accident", "0"),
            Result("c", "T3", "3", null, "Engine", "0")));

        var result = SeasonWrappedService.FindMostDnfs([race]);

        Assert.NotNull(result);
        Assert.Equal("c", result!.DriverId);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void FindMostDnfs_ReturnsNullWhenNobodyRetired()
    {
        var race = Round(1, RaceResult("GP", Result("a", "T", "1", "1", "Finished", "25"), Result("b", "T2", "2", "2", "+1 Lap", "18")));

        Assert.Null(SeasonWrappedService.FindMostDnfs([race]));
    }

    // ----- FindBiggestPointsComeback -----

    [Fact]
    public void FindBiggestPointsComeback_PicksDriverWhoClosedTheBiggestGapToTheLeader()
    {
        var round1 = Round(1, RaceResult("R1", Result("a", "T", "1", "1", "Finished", "25"), Result("b", "T2", "2", "2", "Finished", "0")));
        var round2 = Round(2, RaceResult("R2", Result("a", "T", "1", "5", "Finished", "0"), Result("b", "T2", "5", "1", "Finished", "25")));

        var result = SeasonWrappedService.FindBiggestPointsComeback([round1, round2]);

        // a: gaps [0, 0] -> led round 1, tied round 2 -> comeback 0
        // b: gaps [25, 0] -> max 25, final 0, comeback 25
        Assert.NotNull(result);
        Assert.Equal("b", result!.DriverId);
        Assert.Equal(25, result.Value);
    }

    [Fact]
    public void FindBiggestPointsComeback_ReturnsNullWhenNobodyClosedAnyGap()
    {
        var round1 = Round(1, RaceResult("R1", Result("a", "T", "1", "1", "Finished", "25"), Result("b", "T2", "2", "2", "Finished", "18")));

        // Single round: gap-to-leader is fixed at round 1 for everyone, so max == final for all -> comeback 0 everywhere.
        Assert.Null(SeasonWrappedService.FindBiggestPointsComeback([round1]));
    }

    // ----- FindMostPositionsGainedInARace -----

    [Fact]
    public void FindMostPositionsGainedInARace_PicksTheSingleBiggestGridToFinishJump()
    {
        var race = Round(1, RaceResult(
            "GP",
            Result("a", "T", "10", "1", "Finished", "25"),
            Result("b", "T2", "2", "2", "Finished", "0")));

        var result = SeasonWrappedService.FindMostPositionsGainedInARace([race]);

        Assert.NotNull(result);
        Assert.Equal("a", result!.DriverId);
        Assert.Equal("GP", result.RaceName);
        Assert.Equal(9, result.PositionsGained);
    }

    [Fact]
    public void FindMostPositionsGainedInARace_ReturnsNullWhenNoParseableRows()
    {
        var race = Round(1, RaceResult("GP", Result("a", "T", null, null, "Accident", "0")));

        Assert.Null(SeasonWrappedService.FindMostPositionsGainedInARace([race]));
    }

    // ----- FindMostImprovedConstructorAsync -----

    [Fact]
    public async Task FindMostImprovedConstructorAsync_PicksConstructorWithBiggestPositionGain()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetConstructorStandingsByRoundAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("4", "Williams"), ConstructorStanding("1", "Ferrari")]);
        ergastClient
            .Setup(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("1", "Williams"), ConstructorStanding("2", "Ferrari")]);

        var service = new SeasonWrappedService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var result = await service.FindMostImprovedConstructorAsync(20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Williams", result!.ConstructorName);
        Assert.Equal(4, result.EarlySeasonPosition);
        Assert.Equal(1, result.FinalPosition);
        Assert.Equal(3, result.PositionsImproved);
    }

    [Fact]
    public async Task FindMostImprovedConstructorAsync_UsesRoundFiveOrEarliestAvailableRoundAsCheckpoint()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetConstructorStandingsByRoundAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("1", "Ferrari")]);
        ergastClient
            .Setup(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("1", "Ferrari")]);

        var service = new SeasonWrappedService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        await service.FindMostImprovedConstructorAsync(3, CancellationToken.None);

        ergastClient.Verify(c => c.GetConstructorStandingsByRoundAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }
}
