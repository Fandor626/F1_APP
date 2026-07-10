using CodeHollow.FeedReader;

namespace F1App.Api.Clients;

// Thin wrapper around the CodeHollow.FeedReader static API — mirrors the
// IErgastClient/IOpenF1Client pattern so NewsFeedService can be unit-tested
// with a mock instead of making real network calls.
public class FeedReaderClient : IFeedReaderClient
{
    public async Task<IReadOnlyList<FeedReaderItem>> ReadAsync(string url)
    {
        var feed = await FeedReader.ReadAsync(url);
        return feed.Items.Select(i => new FeedReaderItem(i.Title, i.Link, i.PublishingDate)).ToList();
    }
}
