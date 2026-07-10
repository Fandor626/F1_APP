using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class StandingsServiceTests
{
    private static ErgastDriverStandingDto DriverStanding(string position, string familyName, string points, string constructorName = "Mercedes", string wins = "0", string nationality = "Italian", string driverId = "id") =>
        new(position, points, wins, new ErgastDriverDto(driverId, "Given", familyName, Nationality: nationality), [new ErgastConstructorDto("id", constructorName)]);

    private static ErgastConstructorStandingDto ConstructorStanding(string position, string name, string points, string wins = "0", string nationality = "German") =>
        new(position, points, wins, new ErgastConstructorDto("id", name, nationality));

    private static RaceWeekendSummary Race(int round, string raceName) =>
        new(2026, round, raceName, "Circuit", "City", "Country", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static ErgastResultDto Result(string driverId, string constructorName, string position, string points) =>
        new(new ErgastDriverDto(driverId, "Given", driverId), new ErgastConstructorDto("cid", constructorName), null, position, null, null, points);

    private static ErgastRaceResultRaceDto RaceResult(string raceName, params ErgastResultDto[] results) =>
        new(raceName, "1", "2026-01-01", results);

    [Fact]
    public async Task GetCurrentDriverStandingsAsync_MapsPositionNameConstructorAndPoints()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "Antonelli", "156", "Mercedes", "5", "Italian")]);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var standings = await service.GetCurrentDriverStandingsAsync(CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal(1, standing.Position);
        Assert.Equal("Antonelli", standing.DriverName);
        Assert.Equal("Given Antonelli", standing.FullName);
        Assert.Equal("Mercedes", standing.ConstructorName);
        Assert.Equal(156, standing.Points);
        Assert.Equal(5, standing.Wins);
        Assert.Equal("Italian", standing.Nationality);
    }

    [Fact]
    public async Task GetCurrentDriverStandingsAsync_CachesResultAndDoesNotCallErgastTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "Antonelli", "156")]);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        await service.GetCurrentDriverStandingsAsync(CancellationToken.None);
        await service.GetCurrentDriverStandingsAsync(CancellationToken.None);

        ergastClient.Verify(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentConstructorStandingsAsync_MapsPositionNameAndPoints()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("1", "Mercedes", "262", "6", "German")]);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var standings = await service.GetCurrentConstructorStandingsAsync(CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal(1, standing.Position);
        Assert.Equal("Mercedes", standing.ConstructorName);
        Assert.Equal(262, standing.Points);
        Assert.Equal(6, standing.Wins);
        Assert.Equal("German", standing.Nationality);
    }

    [Fact]
    public async Task GetCurrentConstructorStandingsAsync_CachesResultAndDoesNotCallErgastTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([ConstructorStanding("1", "Mercedes", "262")]);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        await service.GetCurrentConstructorStandingsAsync(CancellationToken.None);
        await service.GetCurrentConstructorStandingsAsync(CancellationToken.None);

        ergastClient.Verify(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChampionshipTrajectoryAsync_SelectsBestPlacedDriverPerTokenizedConstructor()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                DriverStanding("1", "Norris", "25", "McLaren", driverId: "norris"),
                DriverStanding("2", "Verstappen", "18", "Red Bull Racing", driverId: "verstappen"),
                DriverStanding("3", "Leclerc", "15", "Ferrari", driverId: "leclerc"),
                DriverStanding("4", "Piastri", "12", "McLaren", driverId: "piastri"),
                DriverStanding("5", "Russell", "10", "Mercedes", driverId: "russell"),
            ]);
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RaceResult(
                "Bahrain Grand Prix",
                Result("norris", "McLaren", "1", "25"),
                Result("verstappen", "Red Bull Racing", "2", "18"),
                Result("leclerc", "Ferrari", "3", "15"),
                Result("piastri", "McLaren", "4", "12"),
                Result("russell", "Mercedes", "5", "10")));
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastRaceResultRaceDto?)null);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix"), Race(2, "Saudi Arabian Grand Prix") };

        var trajectories = await service.GetChampionshipTrajectoryAsync(schedule, CancellationToken.None);

        Assert.Equal(4, trajectories.Count);
        Assert.Contains(trajectories, t => t.DriverId == "norris");
        Assert.DoesNotContain(trajectories, t => t.DriverId == "piastri");
    }

    [Fact]
    public async Task GetChampionshipTrajectoryAsync_AccumulatesPointsAcrossRounds()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "Norris", "43", "McLaren", driverId: "norris")]);
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RaceResult("Bahrain Grand Prix", Result("norris", "McLaren", "1", "25")));
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RaceResult("Saudi Arabian Grand Prix", Result("norris", "McLaren", "2", "18")));

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix"), Race(2, "Saudi Arabian Grand Prix") };

        var trajectories = await service.GetChampionshipTrajectoryAsync(schedule, CancellationToken.None);

        var norris = Assert.Single(trajectories);
        Assert.Equal(2, norris.Points.Count);
        Assert.Equal(25, norris.Points[0].CumulativePoints);
        Assert.Equal(43, norris.Points[1].CumulativePoints);
        Assert.Equal(18, norris.Points[1].PointsThisRound);
        Assert.Equal("Saudi Arabian Grand Prix", norris.Points[1].RaceName);
    }

    [Fact]
    public async Task GetChampionshipTrajectoryAsync_StopsAtFirstIncompleteRound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "Norris", "43", "McLaren", driverId: "norris")]);
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RaceResult("Bahrain Grand Prix", Result("norris", "McLaren", "1", "25")));
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RaceResult("Saudi Arabian Grand Prix", Result("norris", "McLaren", "2", "18")));
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastRaceResultRaceDto?)null);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix"), Race(2, "Saudi Arabian Grand Prix"), Race(3, "Australian Grand Prix") };

        var trajectories = await service.GetChampionshipTrajectoryAsync(schedule, CancellationToken.None);

        var norris = Assert.Single(trajectories);
        Assert.Equal(2, norris.Points.Count);
        ergastClient.Verify(c => c.GetRaceResultsByRoundAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChampionshipTrajectoryAsync_CachesResultAndDoesNotCallScheduleRoundsTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "Norris", "25", "McLaren", driverId: "norris")]);
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RaceResult("Bahrain Grand Prix", Result("norris", "McLaren", "1", "25")));

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var schedule = new[] { Race(1, "Bahrain Grand Prix") };

        await service.GetChampionshipTrajectoryAsync(schedule, CancellationToken.None);
        await service.GetChampionshipTrajectoryAsync(schedule, CancellationToken.None);

        ergastClient.Verify(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
