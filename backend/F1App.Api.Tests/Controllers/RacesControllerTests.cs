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

public class RacesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RacesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static ErgastRaceDto Race(string round, string date, string raceName) =>
        new(
            "2026",
            round,
            raceName,
            new ErgastCircuitDto("circuit", "Circuit Name", new ErgastLocationDto("City", "Country")),
            date,
            "13:00:00Z",
            FirstPractice: null);

    [Fact]
    public async Task Get_ReturnsScheduleSortedChronologicallyAsCamelCaseJson()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[]
            {
                Race("2", "2026-05-24", "Second Race"),
                Race("1", "2026-03-08", "First Race"),
            }));

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/races");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var schedule = await response.Content.ReadFromJsonAsync<List<RaceWeekendSummary>>();
        Assert.NotNull(schedule);
        Assert.Equal(["First Race", "Second Race"], schedule!.Select(r => r.RaceName));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"raceName\"", body);
        Assert.DoesNotContain("\"RaceName\"", body);
    }

    [Fact]
    public async Task Get_ReturnsBadGatewayProblemDetailsWhenErgastIsUnavailable()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated Ergast outage"));

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/races");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
