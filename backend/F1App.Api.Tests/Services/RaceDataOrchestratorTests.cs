using System.Collections.Concurrent;
using F1App.Api.Clients;
using F1App.Api.Dtos.OpenF1;
using F1App.Api.Hubs;
using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace F1App.Api.Tests.Services;

internal sealed class FakeTimeProvider(DateTimeOffset fixedTime) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => fixedTime;
}

public class RaceDataOrchestratorTests
{
    private static RaceDataOrchestrator CreateOrchestrator(
        TimeProvider? timeProvider = null,
        int joinToleranceMs = 500)
    {
        var mockHub = new Mock<IHubContext<RaceHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group("race")).Returns(mockProxy.Object);

        var mockOpenF1 = new Mock<IOpenF1Client>();

        var mockScope = new Mock<IServiceScope>();
        var mockSp = new Mock<IServiceProvider>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockSp.Object);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JoinToleranceMs"] = joinToleranceMs.ToString(),
            })
            .Build();

        return new RaceDataOrchestrator(
            mockHub.Object,
            mockOpenF1.Object,
            mockScopeFactory.Object,
            config,
            timeProvider ?? TimeProvider.System,
            NullLogger<RaceDataOrchestrator>.Instance);
    }

    private static OpenF1PositionDto MakePosition(int driverNum, int position, DateTimeOffset date) =>
        new(driverNum, position, date);

    private static OpenF1IntervalDto MakeInterval(int driverNum, string? gap, DateTimeOffset date) =>
        new(driverNum, gap, date);

    private static OpenF1StintDto MakeStint(int driverNum, int stintNum, int lapStart,
        string compound, int tyreAgeAtStart = 0) =>
        new(driverNum, stintNum, lapStart, null, compound, tyreAgeAtStart);

    private static LapTimeEntry MakeLap(int lapNum, double? duration, bool isPitOut = false) =>
        new(lapNum, duration, isPitOut);

    private static OpenF1LocationDto MakeLocation(int driverNum, double x, double y, DateTimeOffset date) =>
        new(driverNum, x, y, date);

    private static OpenF1LapDto MakeLapWithSectors(
        int driverNum, int lapNum, bool isPitOut = false,
        double? s1 = null, double? s2 = null, double? s3 = null) =>
        new(driverNum, lapNum, DateTimeOffset.UtcNow,
            s1.HasValue && s2.HasValue && s3.HasValue ? s1 + s2 + s3 : null,
            isPitOut, s1, s2, s3);

    private static DriverStanding MakeStanding(string driverName, decimal points, int position = 1) =>
        new(position, driverName.ToLowerInvariant(), driverName, $"First {driverName}", "Team", points);

    [Fact]
    public void BuildSnapshot_EmptyPositions_ReturnsEmptyDriverList()
    {
        var sut = CreateOrchestrator();

        var snapshot = sut.BuildSnapshot();

        Assert.Empty(snapshot.Drivers);
    }

    [Fact]
    public void BuildSnapshot_DriverWithNoInterval_GapNullAndStale()
    {
        var sut = CreateOrchestrator();
        var now = DateTimeOffset.UtcNow;
        sut._latestPositions[33] = MakePosition(33, 1, now);

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.Drivers);
        Assert.Null(snapshot.Drivers[0].GapToCarAhead);
        Assert.True(snapshot.Drivers[0].GapIsStale);
    }

    [Fact]
    public void BuildSnapshot_DriverWithIntervalWithinTolerance_GapNotStale()
    {
        var sut = CreateOrchestrator(joinToleranceMs: 500);
        var baseTime = DateTimeOffset.UtcNow;

        sut._latestPositions[33] = MakePosition(33, 1, baseTime);
        sut._latestIntervals[33] = MakeInterval(33, "1.234", baseTime.AddMilliseconds(400));

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.Drivers);
        Assert.False(snapshot.Drivers[0].GapIsStale);
        Assert.Equal("1.234", snapshot.Drivers[0].GapToCarAhead);
    }

    [Fact]
    public void BuildSnapshot_DriverWithIntervalOutsideTolerance_GapIsStale()
    {
        var sut = CreateOrchestrator(joinToleranceMs: 500);
        var baseTime = DateTimeOffset.UtcNow;

        sut._latestPositions[33] = MakePosition(33, 1, baseTime);
        sut._latestIntervals[33] = MakeInterval(33, "1.234", baseTime.AddMilliseconds(600));

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.Drivers);
        Assert.True(snapshot.Drivers[0].GapIsStale);
        Assert.Null(snapshot.Drivers[0].GapToCarAhead);
    }

    [Fact]
    public void BuildSnapshot_DriversOrderedByPosition()
    {
        var sut = CreateOrchestrator();
        var now = DateTimeOffset.UtcNow;

        sut._latestPositions[3] = MakePosition(3, 3, now);
        sut._latestPositions[1] = MakePosition(1, 1, now);
        sut._latestPositions[2] = MakePosition(2, 2, now);

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(3, snapshot.Drivers.Count);
        Assert.Equal(1, snapshot.Drivers[0].Position);
        Assert.Equal(2, snapshot.Drivers[1].Position);
        Assert.Equal(3, snapshot.Drivers[2].Position);
    }

    [Fact]
    public void BuildSnapshot_UsesTimeProvider_ForCapturedAt()
    {
        var fixedTime = new DateTimeOffset(2026, 6, 17, 14, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(fixedTime);
        var sut = CreateOrchestrator(timeProvider: fakeTime);

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(fixedTime, snapshot.CapturedAt);
    }

    [Fact]
    public void BuildSnapshot_UsesDriverInfo_ForCodeAndTeam()
    {
        var sut = CreateOrchestrator();
        var now = DateTimeOffset.UtcNow;

        sut._latestPositions[1] = MakePosition(1, 1, now);
        sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
        {
            [1] = new OpenF1DriverInfoDto(1, "VER", "Red Bull Racing", "3671C6"),
        };

        var snapshot = sut.BuildSnapshot();

        Assert.Equal("VER", snapshot.Drivers[0].DriverCode);
        Assert.Equal("Red Bull Racing", snapshot.Drivers[0].TeamName);
        Assert.Equal("3671C6", snapshot.Drivers[0].TeamColour);
    }

    [Fact]
    public void BuildSnapshot_MissingDriverInfo_FallsBackToDriverNumber()
    {
        var sut = CreateOrchestrator();
        var now = DateTimeOffset.UtcNow;
        sut._latestPositions[44] = MakePosition(44, 1, now);

        var snapshot = sut.BuildSnapshot();

        Assert.Equal("44", snapshot.Drivers[0].DriverCode);
        Assert.Equal("555555", snapshot.Drivers[0].TeamColour);
    }

    [Fact]
    public void BuildSnapshot_WithStintData_PopulatesTyreCompound()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestStints[33] = MakeStint(33, 1, 1, "SOFT");

        var snapshot = sut.BuildSnapshot();

        Assert.Equal("SOFT", snapshot.Drivers[0].TyreCompound);
    }

    [Fact]
    public void BuildSnapshot_WithStintAndLapData_PopulatesStintLaps()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestStints[33] = MakeStint(33, 2, 23, "MEDIUM", tyreAgeAtStart: 0);
        sut._driverCurrentLap[33] = 35;

        var snapshot = sut.BuildSnapshot();

        // 0 + Max(0, 35 - 23 + 1) = 13
        Assert.Equal(13, snapshot.Drivers[0].StintLaps);
    }

    [Fact]
    public void BuildSnapshot_NoStintData_TyreFieldsNull()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);

        var snapshot = sut.BuildSnapshot();

        Assert.Null(snapshot.Drivers[0].TyreCompound);
        Assert.Null(snapshot.Drivers[0].StintLaps);
    }

    [Fact]
    public void BuildSnapshot_StintWithNoLapData_StintLapsNull()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestStints[33] = MakeStint(33, 1, 1, "HARD");
        // _driverCurrentLap NOT set

        var snapshot = sut.BuildSnapshot();

        Assert.Equal("HARD", snapshot.Drivers[0].TyreCompound);
        Assert.Null(snapshot.Drivers[0].StintLaps);
    }

    [Fact]
    public void BuildSnapshot_WithLapTimes_PopulatesLapChart()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._driverLapTimes[33] = new ConcurrentDictionary<int, LapTimeEntry>();
        sut._driverLapTimes[33][1] = MakeLap(1, 83.456);
        sut._driverLapTimes[33][2] = MakeLap(2, 82.123);

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.LapChart);
        Assert.Equal(2, snapshot.LapChart[33].Count);
        Assert.Equal(1, snapshot.LapChart[33][0].LapNumber);
        Assert.Equal(83.456, snapshot.LapChart[33][0].LapDurationSeconds);
        Assert.Equal(2, snapshot.LapChart[33][1].LapNumber);
    }

    [Fact]
    public void BuildSnapshot_LapChartOrderedByLapNumber()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._driverLapTimes[33] = new ConcurrentDictionary<int, LapTimeEntry>();
        sut._driverLapTimes[33][3] = MakeLap(3, 81.0);
        sut._driverLapTimes[33][1] = MakeLap(1, 83.0);
        sut._driverLapTimes[33][2] = MakeLap(2, 82.0);

        var snapshot = sut.BuildSnapshot();

        var laps = snapshot.LapChart[33];
        Assert.Equal(1, laps[0].LapNumber);
        Assert.Equal(2, laps[1].LapNumber);
        Assert.Equal(3, laps[2].LapNumber);
    }

    [Fact]
    public void BuildSnapshot_PitOutLapFlagPreserved()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._driverLapTimes[33] = new ConcurrentDictionary<int, LapTimeEntry>();
        sut._driverLapTimes[33][20] = MakeLap(20, 125.7, isPitOut: true);

        var snapshot = sut.BuildSnapshot();

        Assert.True(snapshot.LapChart[33][0].IsPitOutLap);
        Assert.Equal(125.7, snapshot.LapChart[33][0].LapDurationSeconds);
    }

    [Fact]
    public void BuildSnapshot_NoLapTimes_LapChartEmpty()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        // _driverLapTimes not populated

        var snapshot = sut.BuildSnapshot();

        Assert.Empty(snapshot.LapChart);
    }

    [Theory]
    [InlineData(1, 25)]
    [InlineData(2, 18)]
    [InlineData(3, 15)]
    [InlineData(4, 12)]
    [InlineData(5, 10)]
    [InlineData(6, 8)]
    [InlineData(7, 6)]
    [InlineData(8, 4)]
    [InlineData(9, 2)]
    [InlineData(10, 1)]
    [InlineData(11, 0)]
    [InlineData(20, 0)]
    public void RacePointsForPosition_ReturnsCorrectPoints(int position, int expected)
    {
        Assert.Equal(expected, RaceDataOrchestrator.RacePointsForPosition(position));
    }

    [Fact]
    public void BuildSnapshot_LeaderHasPositiveChampionshipDelta()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestPositions[44] = MakePosition(44, 2, DateTimeOffset.UtcNow);
        sut._driverStandings = [MakeStanding("Verstappen", 300), MakeStanding("Hamilton", 250)];
        sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
        {
            [33] = new OpenF1DriverInfoDto(33, "VER", "Red Bull Racing", "3671C6"),
            [44] = new OpenF1DriverInfoDto(44, "HAM", "Mercedes", "27F4D2"),
        };

        var snapshot = sut.BuildSnapshot();

        // VER projected: 300 + 25 = 325; HAM projected: 250 + 18 = 268; lead = 57
        var ver = snapshot.Drivers.Single(d => d.DriverNumber == 33);
        Assert.Equal("+57", ver.ChampionshipDelta);
    }

    [Fact]
    public void BuildSnapshot_TrailerHasNegativeChampionshipDelta()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestPositions[44] = MakePosition(44, 2, DateTimeOffset.UtcNow);
        sut._driverStandings = [MakeStanding("Verstappen", 300), MakeStanding("Hamilton", 250)];
        sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
        {
            [33] = new OpenF1DriverInfoDto(33, "VER", "Red Bull Racing", "3671C6"),
            [44] = new OpenF1DriverInfoDto(44, "HAM", "Mercedes", "27F4D2"),
        };

        var snapshot = sut.BuildSnapshot();

        // HAM projected P2; gap to VER = 325 - 268 = 57
        var ham = snapshot.Drivers.Single(d => d.DriverNumber == 44);
        Assert.Equal("−57", ham.ChampionshipDelta);
    }

    [Fact]
    public void BuildSnapshot_NoStandings_DeltaIsNull()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        // _driverStandings left as empty []

        var snapshot = sut.BuildSnapshot();

        Assert.Null(snapshot.Drivers[0].ChampionshipDelta);
    }

    [Fact]
    public void BuildSnapshot_UnmatchedDriver_DeltaIsNull()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[99] = MakePosition(99, 1, DateTimeOffset.UtcNow);
        sut._driverStandings = [MakeStanding("Verstappen", 300)];
        // Driver 99 has no _driverInfo entry — no acronym for matching

        var snapshot = sut.BuildSnapshot();

        Assert.Null(snapshot.Drivers[0].ChampionshipDelta);
    }

    [Fact]
    public void BuildSnapshot_SingleMatchedDriver_LeaderDeltaIsNull()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._driverStandings = [MakeStanding("Verstappen", 300)];
        sut._driverInfo = new Dictionary<int, OpenF1DriverInfoDto>
        {
            [33] = new OpenF1DriverInfoDto(33, "VER", "Red Bull Racing", "3671C6"),
        };

        var snapshot = sut.BuildSnapshot();

        Assert.Null(snapshot.Drivers[0].ChampionshipDelta);
    }

    // EvaluateSessionMode tests

    [Fact]
    public void EvaluateSessionMode_NoDataYet_ReturnsLive()
    {
        var sut = CreateOrchestrator();
        Assert.Equal(SessionMode.Live, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_RecentData_ReturnsLive()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-5);
        Assert.Equal(SessionMode.Live, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_Data11sAgo_ReturnsStale()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-11);
        Assert.Equal(SessionMode.Stale, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_Data21sAgo_ReturnsFallback()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-21);
        Assert.Equal(SessionMode.Fallback, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_RecoveryFromFallbackFirstGoodPoll_ReturnsStale()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-25);
        sut._sessionMode = SessionMode.Fallback;
        sut._consecutiveGoodPolls = 1;
        Assert.Equal(SessionMode.Stale, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_RecoveryFromStale4GoodPolls_ReturnsLive()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-5);
        sut._sessionMode = SessionMode.Stale;
        sut._consecutiveGoodPolls = 4;
        Assert.Equal(SessionMode.Live, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_StaleWith3GoodPolls_StaysStale()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-5);
        sut._sessionMode = SessionMode.Stale;
        sut._consecutiveGoodPolls = 3;
        Assert.Equal(SessionMode.Stale, sut.EvaluateSessionMode());
    }

    [Fact]
    public void EvaluateSessionMode_StaleDataGoesStaleAgain_ReturnsFallback()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._lastValidPositionTime = now.AddSeconds(-21);
        sut._sessionMode = SessionMode.Stale;
        sut._consecutiveGoodPolls = 0;
        Assert.Equal(SessionMode.Fallback, sut.EvaluateSessionMode());
    }

    // BuildSnapshot in Fallback mode

    [Fact]
    public void BuildSnapshot_FallbackModeWithData_UsesFallbackDrivers()
    {
        var sut = CreateOrchestrator();
        sut._sessionMode = SessionMode.Fallback;
        sut._fallbackRaceName = "Canadian Grand Prix";
        sut._fallbackDrivers =
        [
            new DriverState
            {
                DriverNumber = 4, DriverCode = "NOR", TeamName = "McLaren",
                TeamColour = "555555", Position = 1, GapIsStale = false,
            },
            new DriverState
            {
                DriverNumber = 81, DriverCode = "PIA", TeamName = "McLaren",
                TeamColour = "555555", Position = 2, GapToCarAhead = "+5.014", GapIsStale = false,
            },
        ];

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(SessionMode.Fallback, snapshot.SessionMode);
        Assert.Equal("Canadian Grand Prix", snapshot.FallbackRaceName);
        Assert.Equal(2, snapshot.Drivers.Count);
        Assert.Equal("NOR", snapshot.Drivers[0].DriverCode);
        Assert.Equal("+5.014", snapshot.Drivers[1].GapToCarAhead);
    }

    [Fact]
    public void BuildSnapshot_FallbackModeNoData_ReturnsEmptyDrivers()
    {
        var sut = CreateOrchestrator();
        sut._sessionMode = SessionMode.Fallback;

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(SessionMode.Fallback, snapshot.SessionMode);
        Assert.Empty(snapshot.Drivers);
    }

    [Fact]
    public void BuildSnapshot_LiveMode_SetsSessionModeLive()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._latestPositions[33] = MakePosition(33, 1, now);
        sut._lastValidPositionTime = now.AddSeconds(-2);

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(SessionMode.Live, snapshot.SessionMode);
        Assert.Null(snapshot.FallbackRaceName);
    }

    // Location data tests

    [Fact]
    public void BuildSnapshot_WithLocationData_PopulatesXYOnDriverState()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._latestPositions[1] = MakePosition(1, 1, now);
        sut._latestLocations[1] = MakeLocation(1, -1500.3, 823.1, now);

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.Drivers);
        Assert.Equal(-1500.3, snapshot.Drivers[0].X);
        Assert.Equal(823.1, snapshot.Drivers[0].Y);
    }

    [Fact]
    public void BuildSnapshot_WithoutLocationData_XYAreNull()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._latestPositions[1] = MakePosition(1, 1, now);

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.Drivers);
        Assert.Null(snapshot.Drivers[0].X);
        Assert.Null(snapshot.Drivers[0].Y);
    }

    [Fact]
    public void BuildSnapshot_ActiveCircuitId_IncludedInSnapshot()
    {
        var sut = CreateOrchestrator();
        sut._activeCircuitId = "monza";

        var snapshot = sut.BuildSnapshot();

        Assert.Equal("monza", snapshot.CircuitId);
    }

    [Fact]
    public void ComputeSectorStatus_PitOutLap_SetsWhite()
    {
        var sut = CreateOrchestrator();
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, isPitOut: true, s1: 30.0));
        Assert.Equal("white", sut._latestSectorStatus[1]);
    }

    [Fact]
    public void ComputeSectorStatus_FirstSectorTime_IsSessionBest_SetsPurple()
    {
        var sut = CreateOrchestrator();
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 25.0));
        Assert.Equal("purple", sut._latestSectorStatus[1]);
    }

    [Fact]
    public void ComputeSectorStatus_SecondDriverFasterS1_FirstDriverBecomesGreen()
    {
        var sut = CreateOrchestrator();
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 25.0)); // driver 1: session best → purple
        sut.ComputeSectorStatus(MakeLapWithSectors(2, 1, s1: 24.5)); // driver 2: new session best → purple
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 2, s1: 24.8)); // driver 1: personal best (< 25.0) but not session best → green
        Assert.Equal("purple", sut._latestSectorStatus[2]);
        Assert.Equal("green", sut._latestSectorStatus[1]);
    }

    [Fact]
    public void ComputeSectorStatus_NonBestTime_SetsYellow()
    {
        var sut = CreateOrchestrator();
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 25.0));
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 2, s1: 26.0));
        Assert.Equal("yellow", sut._latestSectorStatus[1]);
    }

    [Fact]
    public void BuildSnapshot_WithSectorStatus_PopulatesMiniSectorStatusOnDriver()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = CreateOrchestrator(timeProvider: new FakeTimeProvider(now));
        sut._latestPositions[1] = MakePosition(1, 1, now);
        sut._latestSectorStatus[1] = "purple";

        var snapshot = sut.BuildSnapshot();

        Assert.Single(snapshot.Drivers);
        Assert.Equal("purple", snapshot.Drivers[0].MiniSectorStatus);
    }

    [Fact]
    public void BuildSnapshot_StintLapsWithinWindow_SetsPitWindowActiveTrue()
    {
        var sut = CreateOrchestrator();
        sut._pitWindowBaselineLaps = 20.0; // MEDIUM window ≈ 17.0–23.0
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestStints[33] = MakeStint(33, 1, 1, "MEDIUM", tyreAgeAtStart: 0);
        sut._driverCurrentLap[33] = 19; // stintLaps = 0 + (19 - 1 + 1) = 19, within [17.0, 23.0]

        var snapshot = sut.BuildSnapshot();

        Assert.True(snapshot.Drivers[0].PitWindowActive);
    }

    [Fact]
    public void BuildSnapshot_StintLapsOutsideWindow_SetsPitWindowActiveFalse()
    {
        var sut = CreateOrchestrator();
        sut._pitWindowBaselineLaps = 20.0;
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        sut._latestStints[33] = MakeStint(33, 1, 1, "MEDIUM", tyreAgeAtStart: 0);
        sut._driverCurrentLap[33] = 5; // stintLaps = 5, well below the window

        var snapshot = sut.BuildSnapshot();

        Assert.False(snapshot.Drivers[0].PitWindowActive);
    }

    [Fact]
    public void BuildSnapshot_NoStintData_PitWindowActiveFalse()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        // no _latestStints entry — tyreCompound stays null

        var snapshot = sut.BuildSnapshot();

        Assert.False(snapshot.Drivers[0].PitWindowActive);
    }

    [Fact]
    public void BuildSnapshot_JustPitted_NewStintResetsWindowToFalse()
    {
        var sut = CreateOrchestrator();
        sut._pitWindowBaselineLaps = 20.0;
        sut._latestPositions[33] = MakePosition(33, 1, DateTimeOffset.UtcNow);
        // Driver was on stint 1 at lap 19 (in window), then pits: stint 2 starts at lap 20 fresh
        sut._latestStints[33] = MakeStint(33, 2, 20, "HARD", tyreAgeAtStart: 0);
        sut._driverCurrentLap[33] = 20; // stintLaps = 0 + (20 - 20 + 1) = 1

        var snapshot = sut.BuildSnapshot();

        Assert.False(snapshot.Drivers[0].PitWindowActive);
    }

    [Fact]
    public void BuildSnapshot_NoSectorTimesYet_FastestSectorsAllNull()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[1] = MakePosition(1, 1, DateTimeOffset.UtcNow);

        var snapshot = sut.BuildSnapshot();

        Assert.NotNull(snapshot.FastestSectors);
        Assert.Null(snapshot.FastestSectors!.S1);
        Assert.Null(snapshot.FastestSectors.S2);
        Assert.Null(snapshot.FastestSectors.S3);
    }

    [Fact]
    public void BuildSnapshot_OneDriverSetsS1_BecomesHolder()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
        sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.5));

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(44, snapshot.FastestSectors!.S1!.DriverNumber);
        Assert.Equal(28.5, snapshot.FastestSectors.S1.TimeSeconds);
    }

    [Fact]
    public void BuildSnapshot_SecondDriverBeatsS1_HolderSwitches()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
        sut._latestPositions[1] = MakePosition(1, 2, DateTimeOffset.UtcNow);
        sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.5));
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.1));

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(1, snapshot.FastestSectors!.S1!.DriverNumber);
        Assert.Equal(28.1, snapshot.FastestSectors.S1.TimeSeconds);
    }

    [Fact]
    public void BuildSnapshot_SlowerSectorTime_DoesNotDisplaceExistingHolder()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
        sut._latestPositions[1] = MakePosition(1, 2, DateTimeOffset.UtcNow);
        sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1));
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5)); // slower — should not take the lead

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(44, snapshot.FastestSectors!.S1!.DriverNumber);
        Assert.Equal(28.1, snapshot.FastestSectors.S1.TimeSeconds);
    }

    [Fact]
    public void BuildSnapshot_AllThreeSectorsIndependent_EachTracksOwnHolder()
    {
        var sut = CreateOrchestrator();
        sut._latestPositions[44] = MakePosition(44, 1, DateTimeOffset.UtcNow);
        sut._latestPositions[1] = MakePosition(1, 2, DateTimeOffset.UtcNow);
        // ComputeSectorStatus only processes the most-recently-completed sector per
        // call (S3 > S2 > S1 priority) — mimic OpenF1's progressive per-sector
        // updates within a lap by calling once per sector as it completes.
        sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1));
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5));
        sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1, s2: 40.0));
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5, s2: 39.5));
        sut.ComputeSectorStatus(MakeLapWithSectors(44, 1, s1: 28.1, s2: 40.0, s3: 25.0));
        sut.ComputeSectorStatus(MakeLapWithSectors(1, 1, s1: 28.5, s2: 39.5, s3: 25.5));

        var snapshot = sut.BuildSnapshot();

        Assert.Equal(44, snapshot.FastestSectors!.S1!.DriverNumber); // 44 faster S1
        Assert.Equal(1, snapshot.FastestSectors.S2!.DriverNumber);   // 1 faster S2
        Assert.Equal(44, snapshot.FastestSectors.S3!.DriverNumber);  // 44 faster S3
    }

    [Fact]
    public void BuildSnapshot_FallbackMode_FastestSectorsIsNull()
    {
        var sut = CreateOrchestrator();
        sut._sessionMode = SessionMode.Fallback;
        sut._fallbackDrivers = [new DriverState { DriverNumber = 1, Position = 1 }];

        var snapshot = sut.BuildSnapshot();

        Assert.Null(snapshot.FastestSectors);
    }
}
