using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F1App.Api.Controllers;
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

        // Case-sensitive key lookup: TryGetProperty does NOT ignore case, so this
        // actually verifies the camelCase policy — unlike deserializing into a
        // record, which matches property names case-insensitively by default.
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("status", out var statusProperty));
        Assert.Equal("ok", statusProperty.GetString());
    }

    [Fact]
    public async Task Get_DeserializesToHealthResponse()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(result);
        Assert.Equal("ok", result!.Status);
    }
}
