using F1App.Api.Clients;
using F1App.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace F1App.Api.Tests.Services;

public class NewsFeedServiceTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    private static NewsFeedService CreateService(Mock<IFeedReaderClient> feedReaderClient, IMemoryCache? cache = null) =>
        new(feedReaderClient.Object, cache ?? new MemoryCache(new MemoryCacheOptions()), EmptyConfig(), NullLogger<NewsFeedService>.Instance);

    [Fact]
    public async Task GetNewsAsync_AggregatesItemsFromAllThreeFeeds()
    {
        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.formula1.com/en/latest/all.xml"))
            .ReturnsAsync([new FeedReaderItem("F1 headline", "https://f1.com/a", DateTime.UtcNow)]);
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.autosport.com/rss/f1/news"))
            .ReturnsAsync([new FeedReaderItem("Autosport headline", "https://autosport.com/a", DateTime.UtcNow)]);
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.racefans.net/feed"))
            .ReturnsAsync([new FeedReaderItem("RaceFans headline", "https://racefans.net/a", DateTime.UtcNow)]);

        var service = CreateService(feedReaderClient);

        var news = await service.GetNewsAsync(CancellationToken.None);

        Assert.Equal(3, news.Count);
        Assert.Contains(news, n => n.Source == "Formula1.com" && n.Title == "F1 headline");
        Assert.Contains(news, n => n.Source == "Autosport" && n.Title == "Autosport headline");
        Assert.Contains(news, n => n.Source == "RaceFans" && n.Title == "RaceFans headline");
    }

    [Fact]
    public async Task GetNewsAsync_OneFeedFailingDoesNotBlockTheOthers()
    {
        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.formula1.com/en/latest/all.xml"))
            .ThrowsAsync(new HttpRequestException("feed down"));
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.autosport.com/rss/f1/news"))
            .ReturnsAsync([new FeedReaderItem("Autosport headline", "https://autosport.com/a", DateTime.UtcNow)]);
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.racefans.net/feed"))
            .ReturnsAsync([new FeedReaderItem("RaceFans headline", "https://racefans.net/a", DateTime.UtcNow)]);

        var service = CreateService(feedReaderClient);

        var news = await service.GetNewsAsync(CancellationToken.None);

        Assert.Equal(2, news.Count);
        Assert.DoesNotContain(news, n => n.Source == "Formula1.com");
    }

    [Fact]
    public async Task GetNewsAsync_AllFeedsDown_ReturnsEmptyListNotAnException()
    {
        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient.Setup(c => c.ReadAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("down"));

        var service = CreateService(feedReaderClient);

        var news = await service.GetNewsAsync(CancellationToken.None);

        Assert.Empty(news);
    }

    [Fact]
    public async Task GetNewsAsync_SortsByPublishedDateDescending()
    {
        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;

        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.formula1.com/en/latest/all.xml"))
            .ReturnsAsync([new FeedReaderItem("Older", "https://f1.com/a", older)]);
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.autosport.com/rss/f1/news"))
            .ReturnsAsync([new FeedReaderItem("Newer", "https://autosport.com/a", newer)]);
        feedReaderClient
            .Setup(c => c.ReadAsync("https://www.racefans.net/feed"))
            .ReturnsAsync([]);

        var service = CreateService(feedReaderClient);

        var news = await service.GetNewsAsync(CancellationToken.None);

        Assert.Equal("Newer", news[0].Title);
        Assert.Equal("Older", news[1].Title);
    }

    [Fact]
    public async Task GetNewsAsync_CachesResultAndDoesNotRefetchOnSecondCall()
    {
        var feedReaderClient = new Mock<IFeedReaderClient>();
        feedReaderClient.Setup(c => c.ReadAsync(It.IsAny<string>())).ReturnsAsync([]);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(feedReaderClient, cache);

        await service.GetNewsAsync(CancellationToken.None);
        await service.GetNewsAsync(CancellationToken.None);

        feedReaderClient.Verify(c => c.ReadAsync(It.IsAny<string>()), Times.Exactly(3)); // 3 feeds, once each — not 6
    }
}
