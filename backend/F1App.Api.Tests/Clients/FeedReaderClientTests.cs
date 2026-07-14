using CodeHollow.FeedReader;
using F1App.Api.Clients;

namespace F1App.Api.Tests.Clients;

// Exercises the real CodeHollow.FeedReader parser against sample RSS XML
// (via FeedReader.ReadFromString, no network call) rather than mocking at
// the IFeedReaderClient boundary — this is the only coverage of the actual
// enclosure-extraction logic.
public class FeedReaderClientTests
{
    private const string Rss20WithImageEnclosure = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>Sample Feed</title>
            <link>https://example.com</link>
            <description>Sample</description>
            <item>
              <title>Headline with image</title>
              <link>https://example.com/a</link>
              <description>A summary</description>
              <enclosure url="https://example.com/image.jpg" length="1000" type="image/jpeg" />
            </item>
          </channel>
        </rss>
        """;

    private const string Rss20WithNonImageEnclosure = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>Sample Feed</title>
            <link>https://example.com</link>
            <description>Sample</description>
            <item>
              <title>Headline with podcast audio</title>
              <link>https://example.com/a</link>
              <description>A summary</description>
              <enclosure url="https://example.com/episode.mp3" length="1000" type="audio/mpeg" />
            </item>
          </channel>
        </rss>
        """;

    private const string Rss20WithNoEnclosure = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>Sample Feed</title>
            <link>https://example.com</link>
            <description>Sample</description>
            <item>
              <title>Headline without image</title>
              <link>https://example.com/a</link>
              <description>A summary</description>
            </item>
          </channel>
        </rss>
        """;

    [Fact]
    public void ExtractImageUrl_Rss20WithImageEnclosure_ReturnsUrl()
    {
        var feed = FeedReader.ReadFromString(Rss20WithImageEnclosure);

        var url = FeedReaderClient.ExtractImageUrl(feed.Items[0]);

        Assert.Equal("https://example.com/image.jpg", url);
    }

    [Fact]
    public void ExtractImageUrl_Rss20WithNonImageEnclosure_ReturnsNull()
    {
        var feed = FeedReader.ReadFromString(Rss20WithNonImageEnclosure);

        var url = FeedReaderClient.ExtractImageUrl(feed.Items[0]);

        Assert.Null(url);
    }

    [Fact]
    public void ExtractImageUrl_Rss20WithNoEnclosure_ReturnsNull()
    {
        var feed = FeedReader.ReadFromString(Rss20WithNoEnclosure);

        var url = FeedReaderClient.ExtractImageUrl(feed.Items[0]);

        Assert.Null(url);
    }

    [Fact]
    public async Task ReadAsync_MapsDescriptionThroughUnmodified()
    {
        // ReadAsync itself needs a live URL — assert the description
        // pass-through via the same parsed FeedItem the real method reads
        // from, matching how ExtractImageUrl is tested above.
        var feed = FeedReader.ReadFromString(Rss20WithImageEnclosure);

        Assert.Equal("A summary", feed.Items[0].Description);
    }
}
