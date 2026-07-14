namespace F1App.Api.Clients;

public record FeedReaderItem(
    string? Title,
    string? Link,
    DateTime? PublishingDate,
    string? ImageUrl = null,
    string? Description = null);

public interface IFeedReaderClient
{
    Task<IReadOnlyList<FeedReaderItem>> ReadAsync(string url);
}
