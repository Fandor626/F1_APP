using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace F1App.Api.Tests.Services;

public class PitWindowServiceTests
{
    private static ErgastPitStopDto MakeStop(string driverId, string lap, string stop) => new(driverId, lap, stop);

    [Fact]
    public void ComputeStintLengths_MultipleDriversAndStops_ComputesLapGapsPerDriver()
    {
        var stops = new List<ErgastPitStopDto>
        {
            MakeStop("norris", "20", "1"),
            MakeStop("norris", "40", "2"),
            MakeStop("piastri", "22", "1"),
        };

        var lengths = PitWindowService.ComputeStintLengths(stops);

        Assert.Equal(3, lengths.Count);
        Assert.Contains(20.0, lengths); // norris stint 1: lap 0 -> 20
        Assert.Contains(22.0, lengths); // piastri stint 1: lap 0 -> 22
    }

    [Theory]
    [InlineData("SOFT", 20.0, 13.6, 18.4)]
    [InlineData("MEDIUM", 20.0, 17.0, 23.0)]
    [InlineData("HARD", 20.0, 21.25, 28.75)]
    [InlineData(null, 20.0, 17.0, 23.0)]
    public void ComputeWindow_ScalesBaselineByCompound(string? compound, double baseline, double expectedMin, double expectedMax)
    {
        var (min, max) = PitWindowService.ComputeWindow(baseline, compound);

        Assert.True(Math.Abs(expectedMin - min) < 0.01, $"expected min {expectedMin}, got {min}");
        Assert.True(Math.Abs(expectedMax - max) < 0.01, $"expected max {expectedMax}, got {max}");
    }

    [Fact]
    public async Task GetBaselineMedianStintLapsAsync_ComputesMedianFromHistoricalPitStops()
    {
        var mockErgast = new Mock<IErgastClient>();
        mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErgastPitStopDto>
            {
                MakeStop("norris", "18", "1"),
                MakeStop("piastri", "22", "1"),
            });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

        var baseline = await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);

        Assert.Equal(20.0, baseline); // median of [18, 22]
    }

    [Fact]
    public async Task GetBaselineMedianStintLapsAsync_NoHistoricalData_ReturnsDefault()
    {
        var mockErgast = new Mock<IErgastClient>();
        mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "new_circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

        var baseline = await sut.GetBaselineMedianStintLapsAsync("new_circuit", 2025, CancellationToken.None);

        Assert.Equal(PitWindowService.DefaultBaselineLaps, baseline);
    }

    [Fact]
    public async Task GetBaselineMedianStintLapsAsync_ErgastThrows_ReturnsDefaultAndDoesNotThrow()
    {
        var mockErgast = new Mock<IErgastClient>();
        mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

        var baseline = await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);

        Assert.Equal(PitWindowService.DefaultBaselineLaps, baseline);
    }

    [Fact]
    public async Task GetBaselineMedianStintLapsAsync_SecondCall_UsesCacheNotErgast()
    {
        var mockErgast = new Mock<IErgastClient>();
        mockErgast.Setup(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErgastPitStopDto> { MakeStop("norris", "20", "1") });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PitWindowService(mockErgast.Object, cache, NullLogger<PitWindowService>.Instance);

        await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);
        await sut.GetBaselineMedianStintLapsAsync("monza", 2025, CancellationToken.None);

        mockErgast.Verify(e => e.GetCircuitPitStopsAsync(2025, "monza", It.IsAny<CancellationToken>()), Times.Once);
    }
}
