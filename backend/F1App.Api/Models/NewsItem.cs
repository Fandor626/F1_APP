namespace F1App.Api.Models;

public record NewsItem(
    string Title,
    string Link,
    string Source,
    DateTimeOffset PublishedAt,
    string? ImageUrl = null,
    string? Snippet = null);
