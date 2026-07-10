using System.Net;
using System.Net.Http.Json;
using F1App.Api.Clients;
using F1App.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace F1App.Api.Tests.Controllers;

public class NewsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NewsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNews_ReturnsCamelCaseNewsItems()
    {
        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.formula1.com/en/latest/all.xml"))
            .ReturnsAsync([new FeedReaderItem("Big F1 headline", "https://f1.com/a", DateTime.UtcNow)]);
        feedReaderClient
            .Setup(c => c.ReadAsync(It.Is<string>(u => u != "https://www.formula1.com/en/latest/all.xml")))
            .ReturnsAsync([]);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IFeedReaderClient>(_ => feedReaderClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var news = await response.Content.ReadFromJsonAsync<List<NewsItem>>();
        var item = Assert.Single(news!);
        Assert.Equal("Big F1 headline", item.Title);
        Assert.Equal("Formula1.com", item.Source);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"publishedAt\"", body);
    }

    [Fact]
    public async Task GetNews_ReturnsEmptyArrayWhenAllFeedsAreDown()
    {
        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient.Setup(c => c.ReadAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("down"));

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IFeedReaderClient>(_ => feedReaderClient.Object)))
            .CreateClient();

        var response = await client.GetAsync("/api/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var news = await response.Content.ReadFromJsonAsync<List<NewsItem>>();
        Assert.Empty(news!);
    }
}
