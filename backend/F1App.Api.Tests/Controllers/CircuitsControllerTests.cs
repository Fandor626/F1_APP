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

public class CircuitsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CircuitsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_ReturnsCamelCaseCircuitProfile()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCircuitInfoAsync("monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastCircuitDto("monza", "Autodromo Nazionale di Monza", new ErgastLocationDto("Monza", "Italy")));
        ergastClient
            .Setup(c => c.GetAllCircuitResultsAsync("monza", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ErgastRaceResultRaceDto("Italian Grand Prix", "16", "2024-09-01",
                [
                    new ErgastResultDto(
                        new ErgastDriverDto("norris", "Lando", "Norris"),
                        new ErgastConstructorDto("mclaren", "McLaren"),
                        null, "1", "4", "Finished", "25", "1",
                        new ErgastFastestLapDto("1", new ErgastResultTimeDto("1:26.720"))),
                ], "2024"),
            ]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/circuits/monza");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<CircuitProfile>();
        Assert.NotNull(profile);
        Assert.Equal("Autodromo Nazionale di Monza", profile!.CircuitName);
        Assert.Single(profile.PastWinners);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"circuitName\"", body);
        Assert.Contains("\"lapRecord\"", body);
        Assert.Contains("\"pastWinners\"", body);
    }

    [Fact]
    public async Task GetProfile_ReturnsNotFoundWhenCircuitUnknown()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCircuitInfoAsync("not_a_circuit", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErgastCircuitDto?)null);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/circuits/not_a_circuit");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
