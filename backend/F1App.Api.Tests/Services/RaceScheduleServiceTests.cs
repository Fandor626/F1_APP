using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class RaceScheduleServiceTests
{
    private static ErgastRaceDto Race(
        string round,
        string date,
        string raceName = "Grand Prix",
        ErgastSessionDto? firstPractice = null,
        ErgastSessionDto? secondPractice = null,
        ErgastSessionDto? thirdPractice = null,
        ErgastSessionDto? qualifying = null,
        ErgastSessionDto? sprint = null,
        ErgastSessionDto? sprintQualifying = null) =>
        new(
            "2026",
            round,
            raceName,
            new ErgastCircuitDto("circuit", "Circuit Name", new ErgastLocationDto("City", "Country")),
            date,
            "13:00:00Z",
            firstPractice,
            secondPractice,
            thirdPractice,
            qualifying,
            sprint,
            sprintQualifying);

    private static ErgastDriverStandingDto DriverStandingDto(string position, string familyName, string points) =>
        new(position, points, "0", new ErgastDriverDto("id", "Given", familyName), [new ErgastConstructorDto("id", "Team")]);

    // Most tests in this file don't exercise the championship-delta path —
    // this gives them a StandingsService that resolves with no driver
    // standings, keeping the constructor call a pure compile-fix for them.
    private static StandingsService EmptyStandingsService()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient.Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    private static StandingsService StandingsServiceWithDrivers(params ErgastDriverStandingDto[] standings)
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient.Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(standings);
        return new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_SortsRacesChronologically()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[]
            {
                Race("3", "2026-09-06", "Third Race"),
                Race("1", "2026-03-08", "First Race"),
                Race("2", "2026-05-24", "Second Race"),
            }));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var schedule = await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        Assert.Equal(["First Race", "Second Race", "Third Race"], schedule.Select(r => r.RaceName));
    }

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_CachesResultAndDoesNotCallErgastTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);
        await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        ergastClient.Verify(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_CombinesDateAndTimeAsUtcDateTimeOffset()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var schedule = await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        Assert.Equal(new DateTimeOffset(2026, 3, 8, 13, 0, 0, TimeSpan.Zero), schedule[0].RaceStart);
    }

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_UsesFirstPracticeAsWeekendStart()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[]
            {
                Race("1", "2026-03-08", firstPractice: new ErgastSessionDto("2026-03-06", "01:30:00Z")),
            }));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var schedule = await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        Assert.Equal(new DateTimeOffset(2026, 3, 6, 1, 30, 0, TimeSpan.Zero), schedule[0].WeekendStart);
    }

    [Fact]
    public async Task GetCurrentSeasonScheduleAsync_FallsBackToRaceDateWhenFirstPracticeIsMissing()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var schedule = await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        Assert.Equal(schedule[0].RaceStart, schedule[0].WeekendStart);
    }

    [Fact]
    public async Task GetRaceDetailAsync_StandardWeekend_OrdersFp1Fp2Fp3QualifyingRace()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[]
            {
                Race(
                    "1",
                    "2026-03-08",
                    firstPractice: new ErgastSessionDto("2026-03-06", "01:30:00Z"),
                    secondPractice: new ErgastSessionDto("2026-03-06", "05:00:00Z"),
                    thirdPractice: new ErgastSessionDto("2026-03-07", "01:30:00Z"),
                    qualifying: new ErgastSessionDto("2026-03-07", "05:00:00Z")),
            }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var detail = await service.GetRaceDetailAsync(1, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(["FP1", "FP2", "FP3", "Qualifying", "Race"], detail!.Sessions.Select(s => s.Name));
    }

    [Fact]
    public async Task GetRaceDetailAsync_SprintWeekend_OrdersFp1SprintQualifyingSprintQualifyingRace()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[]
            {
                Race(
                    "2",
                    "2026-03-15",
                    firstPractice: new ErgastSessionDto("2026-03-13", "03:30:00Z"),
                    qualifying: new ErgastSessionDto("2026-03-14", "07:00:00Z"),
                    sprint: new ErgastSessionDto("2026-03-14", "03:00:00Z"),
                    sprintQualifying: new ErgastSessionDto("2026-03-13", "07:30:00Z")),
            }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var detail = await service.GetRaceDetailAsync(2, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(
            ["FP1", "Sprint Qualifying", "Sprint", "Qualifying", "Race"],
            detail!.Sessions.Select(s => s.Name));
    }

    [Fact]
    public async Task GetRaceDetailAsync_UnknownRound_ReturnsNull()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var detail = await service.GetRaceDetailAsync(999, CancellationToken.None);

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetRaceDetailAsync_PriorYearWinnerPresent_MapsDriverConstructorAndTime()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(2025, "circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ErgastResultDto(
                new ErgastDriverDto("piastri", "Oscar", "Piastri"),
                new ErgastConstructorDto("mclaren", "McLaren"),
                new ErgastResultTimeDto("1:35:39.435"))]);

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var detail = await service.GetRaceDetailAsync(1, CancellationToken.None);

        Assert.NotNull(detail!.PriorYearWinner);
        Assert.Equal("Oscar Piastri", detail.PriorYearWinner!.DriverName);
        Assert.Equal("McLaren", detail.PriorYearWinner!.ConstructorName);
        Assert.Equal("1:35:39.435", detail.PriorYearWinner!.Time);
    }

    [Fact]
    public async Task GetRaceDetailAsync_PriorYearWinnerAbsent_ReturnsNullPriorYearWinner()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(2025, "circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var detail = await service.GetRaceDetailAsync(1, CancellationToken.None);

        Assert.Null(detail!.PriorYearWinner);
    }

    [Fact]
    public async Task GetRaceDetailAsync_CachesCircuitResultsAndDoesNotCallErgastTwice()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(2025, "circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        await service.GetRaceDetailAsync(1, CancellationToken.None);
        await service.GetRaceDetailAsync(1, CancellationToken.None);

        ergastClient.Verify(c => c.GetCircuitResultsAsync(2025, "circuit", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRaceDetailAsync_ChampionshipDelta_ComputedFromTopTwoStandingsSortedByPosition()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Deliberately out of position order to verify the service sorts by
        // Position rather than trusting array order.
        var standingsService = StandingsServiceWithDrivers(
            DriverStandingDto("2", "Verstappen", "289"),
            DriverStandingDto("1", "Norris", "312"));

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), standingsService);

        var detail = await service.GetRaceDetailAsync(1, CancellationToken.None);

        Assert.NotNull(detail!.ChampionshipDelta);
        Assert.Equal("Given Norris", detail.ChampionshipDelta!.LeaderName);
        Assert.Equal("Given Verstappen", detail.ChampionshipDelta!.RunnerUpName);
        Assert.Equal(23, detail.ChampionshipDelta!.PointsGap);
    }

    [Fact]
    public async Task GetRaceDetailAsync_ChampionshipDelta_NullWhenFewerThanTwoStandings()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), EmptyStandingsService());

        var detail = await service.GetRaceDetailAsync(1, CancellationToken.None);

        Assert.Null(detail!.ChampionshipDelta);
    }
}
