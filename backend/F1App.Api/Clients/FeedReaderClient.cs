using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;

namespace F1App.Api.Clients;

// Thin wrapper around the CodeHollow.FeedReader static API — mirrors the
// IErgastClient/IOpenF1Client pattern so NewsFeedService can be unit-tested
// with a mock instead of making real network calls.
public class FeedReaderClient : IFeedReaderClient
{
    public async Task<IReadOnlyList<FeedReaderItem>> ReadAsync(string url)
    {
        var feed = await FeedReader.ReadAsync(url);
        return feed.Items
            .Select(i => new FeedReaderItem(i.Title, i.Link, i.PublishingDate, ExtractImageUrl(i), i.Description))
            .ToList();
    }

    // Image data (AD-11: no second network hop) lives on the format-specific
    // SpecificItem, not the common FeedItem — RSS 2.0's <enclosure> tag is
    // what all three configured feeds actually use, with a media:thumbnail
    // fallback for any feed using the MediaRSS namespace instead.
    internal static string? ExtractImageUrl(FeedItem item)
    {
        return item.SpecificItem switch
        {
            Rss20FeedItem { Enclosure: { } enclosure } when IsImage(enclosure.MediaType) => enclosure.Url,
            Rss092FeedItem { Enclosure: { } enclosure } when IsImage(enclosure.MediaType) => enclosure.Url,
            MediaRssFeedItem mediaItem => mediaItem.Media?.SelectMany(m => m.Thumbnails ?? []).FirstOrDefault()?.Url
                ?? (IsImage(mediaItem.Enclosure?.MediaType) ? mediaItem.Enclosure?.Url : null),
            _ => null,
        };
    }

    private static bool IsImage(string? mediaType) =>
        mediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
}
