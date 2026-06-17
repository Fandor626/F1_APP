using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class WinProbabilityServiceTests
{
    private static ErgastQualifyingResultDto QualifyingResult(string position, string driverId, string givenName, string familyName, string constructor = "Mercedes") =>
        new(position, new ErgastDriverDto(driverId, givenName, familyName), new ErgastConstructorDto("c-id", constructor));

    private static ErgastDriverStandingDto DriverStanding(string position, string driverId, string familyName, string points) =>
        new(position, points, "0", new ErgastDriverDto(driverId, "Given", familyName), [new ErgastConstructorDto("c-id", "Mercedes")]);

    private static (WinProbabilityService service, Mock<IErgastClient> ergastClient) Build()
    {
        var ergastClient = new Mock<IErgastClient>();
        var standingsService = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));
        var service = new WinProbabilityService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()), standingsService);
        return (service, ergastClient);
    }

    [Fact]
    public async Task GetWinProbabilitiesAsync_EmptyQualifying_ReturnsEmptyList()
    {
        var (service, ergastClient) = Build();
        ergastClient
            .Setup(c => c.GetQualifyingResultsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await service.GetWinProbabilitiesAsync(1, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWinProbabilitiesAsync_ThreeDrivers_ProbabilitiesSumToApproximately100()
    {
        var (service, ergastClient) = Build();
        ergastClient
            .Setup(c => c.GetQualifyingResultsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                QualifyingResult("1", "hamilton", "Lewis", "Hamilton"),
                QualifyingResult("2", "verstappen", "Max", "Verstappen", "Red Bull"),
                QualifyingResult("3", "leclerc", "Charles", "Leclerc", "Ferrari"),
            ]);
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                DriverStanding("1", "hamilton", "Hamilton", "150"),
                DriverStanding("2", "verstappen", "Verstappen", "120"),
                DriverStanding("3", "leclerc", "Leclerc", "90"),
            ]);

        var result = await service.GetWinProbabilitiesAsync(1, CancellationToken.None);

        Assert.Equal(3, result.Count);
        var sum = result.Sum(e => e.WinProbability);
        Assert.InRange(sum, 99.0, 101.0);
    }

    [Fact]
    public async Task GetWinProbabilitiesAsync_ChampionInP2_HigherProbabilityThanZeroPointsInP2()
    {
        // With max championship points, P2 driver gets champMultiplier = 1.3
        // vs 1.0 if they had no points — verify their probability is boosted
        var (serviceWithChamp, ergastWithChamp) = Build();
        ergastWithChamp
            .Setup(c => c.GetQualifyingResultsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                QualifyingResult("1", "norris", "Lando", "Norris"),
                QualifyingResult("2", "piastri", "Oscar", "Piastri"),
            ]);
        ergastWithChamp
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                DriverStanding("1", "piastri", "Piastri", "250"),  // P2 driver is championship leader
                DriverStanding("2", "norris", "Norris", "0"),
            ]);

        var (serviceNoPoints, ergastNoPoints) = Build();
        ergastNoPoints
            .Setup(c => c.GetQualifyingResultsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                QualifyingResult("1", "norris", "Lando", "Norris"),
                QualifyingResult("2", "piastri", "Oscar", "Piastri"),
            ]);
        ergastNoPoints
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                DriverStanding("1", "piastri", "Piastri", "0"),
                DriverStanding("2", "norris", "Norris", "0"),
            ]);

        var resultWithChamp = await serviceWithChamp.GetWinProbabilitiesAsync(1, CancellationToken.None);
        var resultNoPoints = await serviceNoPoints.GetWinProbabilitiesAsync(1, CancellationToken.None);

        var p2WithChamp = resultWithChamp.Single(e => e.DriverName == "Oscar Piastri").WinProbability;
        var p2NoPoints = resultNoPoints.Single(e => e.DriverName == "Oscar Piastri").WinProbability;

        Assert.True(p2WithChamp > p2NoPoints, $"Expected championship boost to raise P2 probability ({p2WithChamp} > {p2NoPoints})");
    }

    [Fact]
    public async Task GetWinProbabilitiesAsync_CalledTwiceForSameRound_ErgastCalledOnce()
    {
        var (service, ergastClient) = Build();
        ergastClient
            .Setup(c => c.GetQualifyingResultsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([QualifyingResult("1", "hamilton", "Lewis", "Hamilton")]);
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "hamilton", "Hamilton", "100")]);

        await service.GetWinProbabilitiesAsync(1, CancellationToken.None);
        await service.GetWinProbabilitiesAsync(1, CancellationToken.None);

        ergastClient.Verify(c => c.GetQualifyingResultsAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
