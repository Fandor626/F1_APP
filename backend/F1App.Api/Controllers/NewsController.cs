using F1App.Api.Models;
using F1App.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1App.Api.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController(NewsFeedService newsFeedService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NewsItem>>> GetNews(CancellationToken cancellationToken)
    {
        var news = await newsFeedService.GetNewsAsync(cancellationToken);
        return Ok(news);
    }
}
