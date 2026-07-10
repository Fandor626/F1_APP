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

public class StandingsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StandingsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDrivers_ReturnsCamelCaseStandings()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ErgastDriverStandingDto("1", "156", "5", new ErgastDriverDto("antonelli", "Andrea Kimi", "Antonelli", Nationality: "Italian"), [new ErgastConstructorDto("mercedes", "Mercedes")])]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/standings/drivers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var standings = await response.Content.ReadFromJsonAsync<List<DriverStanding>>();
        var standing = Assert.Single(standings!);
        Assert.Equal("Antonelli", standing.DriverName);
        Assert.Equal(5, standing.Wins);
        Assert.Equal("Italian", standing.Nationality);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"driverName\"", body);
        Assert.Contains("\"wins\"", body);
        Assert.Contains("\"nationality\"", body);
    }

    [Fact]
    public async Task GetConstructors_ReturnsCamelCaseStandings()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentConstructorStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ErgastConstructorStandingDto("1", "262", "6", new ErgastConstructorDto("mercedes", "Mercedes", "German"))]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/standings/constructors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var standings = await response.Content.ReadFromJsonAsync<List<ConstructorStanding>>();
        var standing = Assert.Single(standings!);
        Assert.Equal("Mercedes", standing.ConstructorName);
        Assert.Equal(6, standing.Wins);
        Assert.Equal("German", standing.Nationality);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"constructorName\"", body);
        Assert.Contains("\"wins\"", body);
        Assert.Contains("\"nationality\"", body);
    }

    [Fact]
    public async Task GetTrajectory_ReturnsCamelCaseTrajectory()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026",
            [
                new ErgastRaceDto("2026", "1", "Bahrain Grand Prix",
                    new ErgastCircuitDto("bahrain", "Bahrain International Circuit", new ErgastLocationDto("Sakhir", "Bahrain")),
                    "2026-03-08", "18:00:00Z", null, null, null, null, null, null),
            ]));
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ErgastDriverStandingDto("1", "25", "1", new ErgastDriverDto("norris", "Lando", "Norris"), [new ErgastConstructorDto("mclaren", "McLaren")])]);
        ergastClient
            .Setup(c => c.GetRaceResultsByRoundAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceResultRaceDto("Bahrain Grand Prix", "1", "2026-03-08",
            [
                new ErgastResultDto(new ErgastDriverDto("norris", "Lando", "Norris"), new ErgastConstructorDto("mclaren", "McLaren"), null, "1", "4", "Finished", "25"),
            ]));

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/standings/trajectory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var trajectories = await response.Content.ReadFromJsonAsync<List<DriverTrajectory>>();
        var trajectory = Assert.Single(trajectories!);
        Assert.Equal("Norris", trajectory.DriverName);
        var point = Assert.Single(trajectory.Points);
        Assert.Equal("Bahrain Grand Prix", point.RaceName);
        Assert.Equal(25, point.CumulativePoints);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"cumulativePoints\"", body);
        Assert.Contains("\"raceName\"", body);
    }
}
