using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class RaceScheduleServiceTests
{
    private static ErgastRaceDto Race(string round, string date, string raceName = "Grand Prix", ErgastSessionDto? firstPractice = null) =>
        new(
            "2026",
            round,
            raceName,
            new ErgastCircuitDto("circuit", "Circuit Name", new ErgastLocationDto("City", "Country")),
            date,
            "13:00:00Z",
            firstPractice);

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

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

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

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

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

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

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

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

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

        var service = new RaceScheduleService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var schedule = await service.GetCurrentSeasonScheduleAsync(CancellationToken.None);

        Assert.Equal(schedule[0].RaceStart, schedule[0].WeekendStart);
    }
}
