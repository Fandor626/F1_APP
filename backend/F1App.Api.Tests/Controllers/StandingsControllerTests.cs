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
}
