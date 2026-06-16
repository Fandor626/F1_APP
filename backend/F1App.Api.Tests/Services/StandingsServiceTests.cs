using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace F1App.Api.Tests.Services;

public class StandingsServiceTests
{
    private static ErgastDriverStandingDto DriverStanding(string position, string familyName, string points, string constructorName = "Mercedes") =>
        new(position, points, "0", new ErgastDriverDto("id", "Given", familyName), [new ErgastConstructorDto("id", constructorName)]);

    private static ErgastConstructorStandingDto ConstructorStanding(string position, string name, string points) =>
        new(position, points, "0", new ErgastConstructorDto("id", name));

    [Fact]
    public async Task GetCurrentDriverStandingsAsync_MapsPositionNameConstructorAndPoints()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DriverStanding("1", "Antonelli", "156", "Mercedes")]);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var standings = await service.GetCurrentDriverStandingsAsync(CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal(1, standing.Position);
        Assert.Equal("Antonelli", standing.DriverName);
        Assert.Equal("Mercedes", standing.ConstructorName);
        Assert.Equal(156, standing.Points);
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
            .ReturnsAsync([ConstructorStanding("1", "Mercedes", "262")]);

        var service = new StandingsService(ergastClient.Object, new MemoryCache(new MemoryCacheOptions()));

        var standings = await service.GetCurrentConstructorStandingsAsync(CancellationToken.None);

        var standing = Assert.Single(standings);
        Assert.Equal(1, standing.Position);
        Assert.Equal("Mercedes", standing.ConstructorName);
        Assert.Equal(262, standing.Points);
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
}
