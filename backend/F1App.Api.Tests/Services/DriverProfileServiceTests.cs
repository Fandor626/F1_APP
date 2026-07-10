using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class DriverProfileServiceTests
{
    private static ErgastDriverDto DriverInfo(string driverId = "max_verstappen", string given = "Max", string family = "Verstappen", string nationality = "Dutch") =>
        new(driverId, given, family, Nationality: nationality);

    private static ErgastResultDto Result(
        string driverId, string constructorName, string? position, string? grid, string status = "Finished",
        string points = "0", string? fastestLapRank = null) =>
        new(
            new ErgastDriverDto(driverId, "Given", driverId),
            new ErgastConstructorDto("cid", constructorName),
            null,
            position,
            null,
            status,
            points,
            grid,
            fastestLapRank is null ? null : new ErgastFastestLapDto(fastestLapRank, new ErgastResultTimeDto("1:30.000")));

    private static ErgastRaceResultRaceDto Race(string season, string round, string raceName, ErgastResultDto result) =>
        new(raceName, round, "2026-01-01", [result], season);

    private static ErgastDriverStandingDto ChampionStanding(string driverId) =>
        new("1", "400", "10", new ErgastDriverDto(driverId, "Given", driverId), [new ErgastConstructorDto("cid", "Team")]);

    [Fact]
    public async Task GetDriverProfileAsync_ReturnsNullWhenDriverInfoNotFound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("not_a_driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastDriverDto?)null);

        var service = new DriverProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetDriverProfileAsync("not_a_driver", CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task GetDriverProfileAsync_ComputesCareerTotals()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverInfo());
        ergastClient
            .Setup(c => c.GetAllDriverResultsAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                // Win + pole + fastest lap, all in one race
                Race("2023", "1", "Race A", Result("max_verstappen", "Red Bull", "1", "1", "Finished", "25", "1")),
                // Podium (P2) but not a win, pole set by someone else (grid 3), no fastest lap
                Race("2023", "2", "Race B", Result("max_verstappen", "Red Bull", "2", "3", "Finished", "18")),
                // Not a podium, not a pole, not a fastest lap
                Race("2023", "3", "Race C", Result("max_verstappen", "Red Bull", "7", "8", "Finished", "6")),
            ]);
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(2023, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChampionStanding("max_verstappen"));

        var service = new DriverProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetDriverProfileAsync("max_verstappen", CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(3, profile!.CareerTotals.Races);
        Assert.Equal(1, profile.CareerTotals.Wins);
        Assert.Equal(2, profile.CareerTotals.Podiums); // P1 and P2
        Assert.Equal(1, profile.CareerTotals.Poles);
        Assert.Equal(1, profile.CareerTotals.FastestLaps);
        Assert.Equal(1, profile.CareerTotals.Titles);
    }

    [Fact]
    public async Task GetDriverProfileAsync_CountsTitlesFromDistinctSeasonsOnly()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverInfo());
        ergastClient
            .Setup(c => c.GetAllDriverResultsAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Race("2022", "1", "Race A", Result("max_verstappen", "Red Bull", "1", "1")),
                Race("2022", "2", "Race B", Result("max_verstappen", "Red Bull", "1", "1")),
                Race("2023", "1", "Race C", Result("max_verstappen", "Red Bull", "1", "1")),
            ]);
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(2022, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChampionStanding("max_verstappen"));
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(2023, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChampionStanding("someone_else"));

        var service = new DriverProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetDriverProfileAsync("max_verstappen", CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(1, profile!.CareerTotals.Titles); // only 2022, not 2023
        ergastClient.Verify(c => c.GetSeasonChampionAsync(2022, It.IsAny<CancellationToken>()), Times.Once);
        ergastClient.Verify(c => c.GetSeasonChampionAsync(2023, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDriverProfileAsync_BuildsConstructorHistoryGroupedBySeasonWithoutDuplicates()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverInfo("driver"));
        ergastClient
            .Setup(c => c.GetAllDriverResultsAsync("driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Race("2020", "1", "Race A", Result("driver", "Team A", "5", "5")),
                Race("2020", "2", "Race B", Result("driver", "Team A", "6", "6")),
                Race("2021", "1", "Race C", Result("driver", "Team B", "4", "4")),
            ]);
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChampionStanding("someone_else"));

        var service = new DriverProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetDriverProfileAsync("driver", CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(2, profile!.ConstructorHistory.Count);
        Assert.Equal(2020, profile.ConstructorHistory[0].Season);
        Assert.Equal(["Team A"], profile.ConstructorHistory[0].ConstructorNames);
        Assert.Equal(2021, profile.ConstructorHistory[1].Season);
        Assert.Equal(["Team B"], profile.ConstructorHistory[1].ConstructorNames);
    }

    [Fact]
    public async Task GetDriverProfileAsync_BuildsCumulativeCareerPointsInChronologicalOrder()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverInfo("driver"));
        ergastClient
            // Deliberately returned out of order to prove the service re-sorts.
            .Setup(c => c.GetAllDriverResultsAsync("driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Race("2021", "1", "Race C", Result("driver", "Team", "1", "1", "Finished", "25")),
                Race("2020", "2", "Race B", Result("driver", "Team", "2", "2", "Finished", "18")),
                Race("2020", "1", "Race A", Result("driver", "Team", "3", "3", "Finished", "15")),
            ]);
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastDriverStandingDto?)null);

        var service = new DriverProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var profile = await service.GetDriverProfileAsync("driver", CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(3, profile!.CareerPoints.Count);
        Assert.Equal("Race A", profile.CareerPoints[0].RaceName);
        Assert.Equal(15, profile.CareerPoints[0].CumulativePoints);
        Assert.Equal("Race B", profile.CareerPoints[1].RaceName);
        Assert.Equal(33, profile.CareerPoints[1].CumulativePoints);
        Assert.Equal("Race C", profile.CareerPoints[2].RaceName);
        Assert.Equal(58, profile.CareerPoints[2].CumulativePoints);
    }

    [Fact]
    public async Task GetDriverProfileAsync_CachesResultAndDoesNotCallErgastTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DriverInfo("driver"));
        ergastClient
            .Setup(c => c.GetAllDriverResultsAsync("driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Race("2020", "1", "Race A", Result("driver", "Team", "1", "1"))]);
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastDriverStandingDto?)null);

        var service = new DriverProfileService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        await service.GetDriverProfileAsync("driver", CancellationToken.None);
        await service.GetDriverProfileAsync("driver", CancellationToken.None);

        ergastClient.Verify(c => c.GetDriverInfoAsync("driver", It.IsAny<CancellationToken>()), Times.Once);
    }
}
