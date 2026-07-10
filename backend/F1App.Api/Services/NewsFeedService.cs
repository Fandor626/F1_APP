using F1App.Api.Clients;
using F1App.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace F1App.Api.Services;

public class NewsFeedService(
    IFeedReaderClient feedReaderClient,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<NewsFeedService> logger)
{
    private const string CacheKey = "news:feed";

    // Verified live before implementation — formula1.com's actual feed path
    // differs from epics.md's stale URL (see Story 6.1's Dev Notes).
    private static readonly (string Name, string Url)[] Feeds =
    [
        ("Formula1.com", "https://www.formula1.com/en/latest/all.xml"),
        ("Autosport", "https://www.autosport.com/rss/f1/news"),
        ("RaceFans", "https://www.racefans.net/feed"),
    ];

    public async Task<IReadOnlyList<NewsItem>> GetNewsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<NewsItem>? cached) && cached is not null)
            return cached;

        var refreshMinutes = configuration.GetValue("NewsFeedRefreshIntervalMinutes", 15);
        var items = new List<NewsItem>();

        foreach (var (name, url) in Feeds)
        {
            try
            {
                var feedItems = await feedReaderClient.ReadAsync(url);
                items.AddRange(feedItems.Select(i => new NewsItem(
                    i.Title ?? "(untitled)",
                    i.Link ?? "",
                    name,
                    i.PublishingDate is { } date ? new DateTimeOffset(date, TimeSpan.Zero) : DateTimeOffset.UtcNow)));
            }
            catch (Exception ex)
            {
                // Per-feed error isolation — one broken feed must not prevent
                // the others' headlines from rendering.
                logger.LogWarning(ex, "Failed to fetch news feed {FeedName} from {Url}", name, url);
            }
        }

        var sorted = items.OrderByDescending(i => i.PublishedAt).ToList();
        cache.Set(CacheKey, (IReadOnlyList<NewsItem>)sorted, TimeSpan.FromMinutes(refreshMinutes));
        return sorted;
    }
}
