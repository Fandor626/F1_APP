using F1App.Api.Clients;
using F1App.Api.Dtos.OpenF1;
using F1App.Api.Hubs;
using F1App.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JoinToleranceMs"] = joinToleranceMs.ToString(),
            })
            .Build();

        return new RaceDataOrchestrator(
            mockHub.Object,
            mockOpenF1.Object,
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
}
