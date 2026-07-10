using System.Net;
using System.Net.Http.Json;
using F1App.Api.Clients;
using F1App.Api.Dtos.Ergast;
using F1App.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace F1App.Api.Tests.Controllers;

public class DriversControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DriversControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_ReturnsCamelCaseDriverProfile()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastDriverDto("max_verstappen", "Max", "Verstappen", Nationality: "Dutch"));
        ergastClient
            .Setup(c => c.GetAllDriverResultsAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ErgastRaceResultRaceDto("Bahrain Grand Prix", "1", "2023-03-05",
                [
                    new ErgastResultDto(
                        new ErgastDriverDto("max_verstappen", "Max", "Verstappen"),
                        new ErgastConstructorDto("red_bull", "Red Bull"),
                        null, "1", "1", "Finished", "25", "1",
                        new ErgastFastestLapDto("1", new ErgastResultTimeDto("1:32.000"))),
                ], "2023"),
            ]);
        ergastClient
            .Setup(c => c.GetSeasonChampionAsync(2023, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastDriverStandingDto("1", "575", "19",
                new ErgastDriverDto("max_verstappen", "Max", "Verstappen"), [new ErgastConstructorDto("red_bull", "Red Bull")]));

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/drivers/max_verstappen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<DriverProfile>();
        Assert.NotNull(profile);
        Assert.Equal("Max Verstappen", profile!.FullName);
        Assert.Equal(1, profile.CareerTotals.Wins);
        Assert.Equal(1, profile.CareerTotals.Titles);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"careerTotals\"", body);
        Assert.Contains("\"constructorHistory\"", body);
        Assert.Contains("\"careerPoints\"", body);
    }

    [Fact]
    public async Task GetProfile_ReturnsNotFoundWhenDriverUnknown()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetDriverInfoAsync("not_a_driver", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastDriverDto?)null);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/drivers/not_a_driver");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllDrivers_ReturnsCamelCaseDriverOptions()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetAllDriversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ErgastDriverDto("max_verstappen", "Max", "Verstappen")]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/drivers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var drivers = await response.Content.ReadFromJsonAsync<List<DriverOption>>();
        var driver = Assert.Single(drivers!);
        Assert.Equal("Max Verstappen", driver.FullName);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"driverId\"", body);
        Assert.Contains("\"fullName\"", body);
    }

    [Fact]
    public async Task Compare_ReturnsCamelCaseComparison()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient.Setup(c => c.GetDriverInfoAsync("max_verstappen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastDriverDto("max_verstappen", "Max", "Verstappen"));
        ergastClient.Setup(c => c.GetDriverInfoAsync("hamilton", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastDriverDto("hamilton", "Lewis", "Hamilton"));
        ergastClient.Setup(c => c.GetFilteredDriverResultsAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ergastClient.Setup(c => c.GetDriverQualifyingHistoryAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/drivers/compare?driverA=max_verstappen&driverB=hamilton");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comparison = await response.Content.ReadFromJsonAsync<HeadToHeadComparison>();
        Assert.NotNull(comparison);
        Assert.Equal("Max Verstappen", comparison!.DriverA.FullName);
        Assert.Equal("Lewis Hamilton", comparison.DriverB.FullName);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"qualifyingAveragePosition\"", body);
        Assert.Contains("\"raceFinishAveragePosition\"", body);
    }
}
