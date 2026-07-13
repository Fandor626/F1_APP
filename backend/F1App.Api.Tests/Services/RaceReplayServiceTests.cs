using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Dtos.OpenF1;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace F1App.Api.Tests.Services;

public class RaceReplayServiceTests
{
    private static RaceReplayService CreateService(
        Mock<IErgastClient> ergastMock, Mock<IOpenF1Client> openF1Mock, IMemoryCache? cache = null) =>
        new(ergastMock.Object, openF1Mock.Object, cache ?? new MemoryCache(new MemoryCacheOptions()), NullLogger<RaceReplayService>.Instance);

    private static ErgastRaceResultRaceDto MakeRaceResult(string date = "2026-09-07") =>
        new("Italian Grand Prix", "1", date, [], "2026");

    private static OpenF1SessionDto MakeSession(int sessionKey, DateTimeOffset dateStart) =>
        new(sessionKey, "Race", dateStart);

    private static OpenF1LapDto MakeLap(
        int driverNum, int lapNum, double duration, double? s1 = null, double? s2 = null, double? s3 = null) =>
        new(driverNum, lapNum, DateTimeOffset.UtcNow, duration, false, s1, s2, s3);

    private void SetupHistoricalEndpoints(
        Mock<IOpenF1Client> openF1Mock,
        int sessionKey,
        IReadOnlyList<OpenF1LapDto> laps,
        IReadOnlyList<OpenF1StintDto>? stints = null,
        IReadOnlyList<OpenF1RaceControlDto>? raceControl = null,
        IReadOnlyList<OpenF1PitDto>? pit = null,
        IReadOnlyList<OpenF1DriverInfoDto>? drivers = null)
    {
        openF1Mock.Setup(c => c.GetLapsForSessionAsync(sessionKey, It.IsAny<CancellationToken>())).ReturnsAsync(laps);
        openF1Mock.Setup(c => c.GetStintsForSessionAsync(sessionKey, It.IsAny<CancellationToken>())).ReturnsAsync(stints ?? []);
        openF1Mock.Setup(c => c.GetRaceControlForSessionAsync(sessionKey, It.IsAny<CancellationToken>())).ReturnsAsync(raceControl ?? []);
        openF1Mock.Setup(c => c.GetPitStopsForSessionAsync(sessionKey, It.IsAny<CancellationToken>())).ReturnsAsync(pit ?? []);
        openF1Mock.Setup(c => c.GetDriversForSessionAsync(sessionKey, It.IsAny<CancellationToken>())).ReturnsAsync(drivers ?? []);
    }

    [Fact]
    public async Task GetReplayAsync_RoundNotFound_ReturnsNull()
    {
        var ergast = new Mock<IErgastClient>();
        ergast.Setup(c => c.GetRaceResultsByRoundAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastRaceResultRaceDto?)null);
        var openF1 = new Mock<IOpenF1Client>();

        var sut = CreateService(ergast, openF1);

        var result = await sut.GetReplayAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReplayAsync_NoMatchingOpenF1Session_ReturnsNull()
    {
        var ergast = new Mock<IErgastClient>();
        ergast.Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRaceResult());
        var openF1 = new Mock<IOpenF1Client>();
        openF1.Setup(c => c.GetRaceSessionsAsync(2026, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = CreateService(ergast, openF1);

        var result = await sut.GetReplayAsync(1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReplayAsync_BuildsOneFrameFromEachLap_RankedByCumulativeTime()
    {
        var ergast = new Mock<IErgastClient>();
        ergast.Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRaceResult());
        var openF1 = new Mock<IOpenF1Client>();
        var raceDate = new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero);
        openF1.Setup(c => c.GetRaceSessionsAsync(2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeSession(9999, raceDate)]);

        // Driver 1: laps of 90s, 91s (cumulative 90, 181)
        // Driver 44: laps of 92s, 89s (cumulative 92, 181) — driver 1 leads lap 1, tied cumulative at lap 2
        SetupHistoricalEndpoints(openF1, 9999,
        [
            MakeLap(1, 1, 90, s1: 30, s2: 30, s3: 30),
            MakeLap(1, 2, 91, s1: 30, s2: 30, s3: 31),
            MakeLap(44, 1, 92, s1: 31, s2: 30, s3: 31),
            MakeLap(44, 2, 89, s1: 29, s2: 29, s3: 31),
        ]);

        var sut = CreateService(ergast, openF1);

        var frames = await sut.GetReplayAsync(1, CancellationToken.None);

        Assert.NotNull(frames);
        Assert.Equal(2, frames!.Count);

        var lap1 = frames[0];
        Assert.Equal(2, lap1.Drivers.Count);
        Assert.Equal(1, lap1.Drivers[0].DriverNumber); // 90s < 92s
        Assert.Equal(1, lap1.Drivers[0].Position);
        Assert.Null(lap1.Drivers[0].GapToCarAhead);
        Assert.Equal(44, lap1.Drivers[1].DriverNumber);
        Assert.Equal("+2.000", lap1.Drivers[1].GapToCarAhead);

        var lap2 = frames[1];
        // Driver 1: 90+91=181, Driver 44: 92+89=181 — exact tie, stable order by prior comparison
        Assert.Equal(2, lap2.Drivers.Count);
        Assert.NotNull(lap2.FastestSectors!.S1); // best S1 across laps seen so far
    }

    [Fact]
    public async Task GetReplayAsync_RetiredDriverStopsAdvancing_FallsBehindInLaterFrames()
    {
        var ergast = new Mock<IErgastClient>();
        ergast.Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRaceResult());
        var openF1 = new Mock<IOpenF1Client>();
        openF1.Setup(c => c.GetRaceSessionsAsync(2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeSession(9999, new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero))]);

        // Driver 1 completes 3 laps; driver 44 retires after lap 1.
        SetupHistoricalEndpoints(openF1, 9999,
        [
            MakeLap(1, 1, 90), MakeLap(1, 2, 90), MakeLap(1, 3, 90),
            MakeLap(44, 1, 91),
        ]);

        var sut = CreateService(ergast, openF1);

        var frames = await sut.GetReplayAsync(1, CancellationToken.None);

        Assert.NotNull(frames);
        Assert.Equal(3, frames!.Count);
        // By lap 3, driver 1 has done 270s, driver 44 is frozen at 91s from lap 1 —
        // but driver 44 still only "counts" through their own last completed lap,
        // so they remain present (not silently dropped) and rank behind.
        var lap3 = frames[2].Drivers;
        Assert.Equal(2, lap3.Count);
        Assert.Equal(1, lap3[0].DriverNumber);
        Assert.Equal(44, lap3[1].DriverNumber);
    }

    [Fact]
    public async Task GetReplayAsync_PopulatesTyreCompoundFromStints()
    {
        var ergast = new Mock<IErgastClient>();
        ergast.Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRaceResult());
        var openF1 = new Mock<IOpenF1Client>();
        openF1.Setup(c => c.GetRaceSessionsAsync(2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeSession(9999, new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero))]);
        SetupHistoricalEndpoints(openF1, 9999,
            [MakeLap(1, 1, 90), MakeLap(1, 2, 90)],
            stints: [new OpenF1StintDto(1, 1, 1, null, "MEDIUM", 0)]);

        var sut = CreateService(ergast, openF1);

        var frames = await sut.GetReplayAsync(1, CancellationToken.None);

        Assert.NotNull(frames);
        Assert.Equal("MEDIUM", frames![1].Drivers[0].TyreCompound);
        Assert.Equal(2, frames[1].Drivers[0].StintLaps);
    }

    [Fact]
    public async Task GetReplayAsync_CachesResultAndDoesNotRebuildTwice()
    {
        var ergast = new Mock<IErgastClient>();
        ergast.Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRaceResult());
        var openF1 = new Mock<IOpenF1Client>();
        openF1.Setup(c => c.GetRaceSessionsAsync(2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeSession(9999, new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero))]);
        SetupHistoricalEndpoints(openF1, 9999, [MakeLap(1, 1, 90)]);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateService(ergast, openF1, cache);

        await sut.GetReplayAsync(1, CancellationToken.None);
        await sut.GetReplayAsync(1, CancellationToken.None);

        ergast.Verify(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
