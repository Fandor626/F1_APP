using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace F1App.Api.Tests.Controllers;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsOkWithCamelCaseStatusField()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
    }

    [Fact]
    public async Task Get_DeserializesToHealthResponse()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<HealthResponseDto>("/api/health");

        Assert.NotNull(result);
        Assert.Equal("ok", result!.Status);
    }

    private sealed record HealthResponseDto(string Status);
}
