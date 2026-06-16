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
            FirstPractice: null,
            SecondPractice: null,
            ThirdPractice: null,
            Qualifying: null,
            Sprint: null,
            SprintQualifying: null);

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

    [Fact]
    public async Task GetDetail_ReturnsSessionsForMatchingRound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08", "First Race") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/races/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<RaceWeekendDetail>();
        Assert.NotNull(detail);
        Assert.Equal("First Race", detail!.RaceName);
        Assert.Equal(["Race"], detail.Sessions.Select(s => s.Name));
    }

    [Fact]
    public async Task GetDetail_ReturnsContextualDataAsCamelCaseJsonAndOmitsNullPriorYearWinner()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08", "First Race") }));
        ergastClient
            .Setup(c => c.GetCircuitResultsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ergastClient
            .Setup(c => c.GetCurrentDriverStandingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ErgastDriverStandingDto("1", "312", "10", new ErgastDriverDto("norris", "Lando", "Norris"), [new ErgastConstructorDto("mclaren", "McLaren")]),
                new ErgastDriverStandingDto("2", "289", "8", new ErgastDriverDto("verstappen", "Max", "Verstappen"), [new ErgastConstructorDto("red_bull", "Red Bull Racing")]),
            ]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/races/1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"priorYearWinner\"", body);
        Assert.Contains("\"championshipDelta\"", body);

        var detail = await response.Content.ReadFromJsonAsync<RaceWeekendDetail>();
        Assert.Null(detail!.PriorYearWinner);
        Assert.NotNull(detail.ChampionshipDelta);
        Assert.Equal("Norris", detail.ChampionshipDelta!.LeaderName);
        Assert.Equal("Verstappen", detail.ChampionshipDelta!.RunnerUpName);
        Assert.Equal(23, detail.ChampionshipDelta!.PointsGap);
    }

    [Fact]
    public async Task GetDetail_ReturnsNotFoundForUnknownRound()
    {
        var ergastClient = new Mock<IErgastClient>();
        ergastClient
            .Setup(c => c.GetCurrentSeasonScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErgastRaceTableDto("2026", new[] { Race("1", "2026-03-08", "First Race") }));

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IErgastClient>(_ => ergastClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/races/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
