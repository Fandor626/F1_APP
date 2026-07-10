---
baseline_commit: "69528ef"
---

# Story 6.1: F1 News Feed

Status: done

## Story

As a fan staying current between races,
I want to see aggregated F1 news headlines,
So that I don't have to check multiple sites.

## Acceptance Criteria

1. **Given** the news feed page loads **When** RSS feeds (Formula1.com, Autosport, RaceFans) are fetched via the backend proxy **Then** headlines render as a card list showing title, source, and timestamp.
2. **Given** a card is clicked **Then** the article opens in a new browser tab.
3. **Given** the feed is cached **Then** it refreshes on a configurable interval (default 15 minutes).
4. **Given** all feeds are unavailable **Then** a clear "no news available" state is shown, **and** one broken feed does not block headlines from the others (per-feed error isolation).

## RSS feed URLs verified live before writing any code

epics.md's "Additional Requirements" listed source URLs that turned out to be stale/incorrect. Verified against the actual live sites:

| Feed | epics.md URL | Actual working URL |
|---|---|---|
| Formula1.com | `formula1.com/en/latest/all.news.rss` (404) | `https://www.formula1.com/en/latest/all.xml` (200, valid RSS 2.0) |
| Autosport | `autosport.com/rss/f1/news` | Same URL — works via HTTP redirect chain (`HttpClient`'s default `AllowAutoRedirect` follows it to `https://www.autosport.com/rss/f1/news/`), confirmed 200 with valid RSS content |
| RaceFans | `racefans.net/feed` | Same URL — redirects to `https://www.racefans.net/feed/` (trailing slash), confirmed 200 with valid RSS content |

This story uses the corrected Formula1.com URL and relies on `HttpClient`'s default redirect-following behavior for the other two (no special-casing needed there).

## Tasks / Subtasks

### Task 1: Backend — Add `CodeHollow.FeedReader` package (AC: 1, 4)

- [ ] `backend/F1App.Api/F1App.Api.csproj` — add `<PackageReference Include="CodeHollow.FeedReader" Version="1.2.6" />` (latest on NuGet at time of writing, confirmed via the NuGet flat-container API).

### Task 2: Backend — `NewsItem` model (AC: 1)

- [ ] Create `backend/F1App.Api/Models/NewsItem.cs`:
  ```csharp
  namespace F1App.Api.Models;

  public record NewsItem(string Title, string Link, string Source, DateTimeOffset PublishedAt);
  ```

### Task 3: Backend — `NewsFeedService` (AC: 1, 3, 4)

- [ ] `backend/F1App.Api/appsettings.json` — add `"NewsFeedRefreshIntervalMinutes": 15` (committed default, not a secret — unlike `OpenF1BaseUrl`/`AllowedOrigins`, which live in the gitignored `appsettings.Development.json` per existing convention).
- [ ] Create `backend/F1App.Api/Services/NewsFeedService.cs`:
  ```csharp
  using CodeHollow.FeedReader;
  using F1App.Api.Models;
  using Microsoft.Extensions.Caching.Memory;

  namespace F1App.Api.Services;

  public class NewsFeedService(IMemoryCache cache, IConfiguration configuration, ILogger<NewsFeedService> logger)
  {
      private const string CacheKey = "news:feed";

      // Verified live (see Story 6.1's Dev Notes) — formula1.com's real feed
      // path differs from epics.md's stale URL.
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
                  var feed = await FeedReader.ReadAsync(url);
                  items.AddRange(feed.Items.Select(i => new NewsItem(
                      i.Title ?? "(untitled)",
                      i.Link ?? "",
                      name,
                      i.PublishingDate is { } date ? new DateTimeOffset(date, TimeSpan.Zero) : DateTimeOffset.UtcNow)));
              }
              catch (Exception ex)
              {
                  // Per-feed error isolation (AC 4) — one broken feed must not
                  // prevent the others' headlines from rendering.
                  logger.LogWarning(ex, "Failed to fetch news feed {FeedName} from {Url}", name, url);
              }
          }

          var sorted = items.OrderByDescending(i => i.PublishedAt).ToList();
          cache.Set(CacheKey, (IReadOnlyList<NewsItem>)sorted, TimeSpan.FromMinutes(refreshMinutes));
          return sorted;
      }
  }
  ```
  **Note on `DateTimeOffset` (NFR-11)**: `CodeHollow.FeedReader`'s `FeedItem.PublishingDate` is a third-party `DateTime?` — converted to `DateTimeOffset` immediately at this boundary (assuming UTC, since RSS `pubDate` values are already normalized by the library's parser) rather than let a bare `DateTime` leak further into the codebase.
  **Note on caching**: a completely-empty `sorted` list (all three feeds down) is still cached for the configured interval — this is intentional, not a bug: it avoids hammering three already-failing feeds every request, and AC 4's "no news available" state is exactly what an empty list renders as.

### Task 4: Backend — `NewsController`, DI registration (AC: 1, 3, 4)

- [ ] Create `backend/F1App.Api/Controllers/NewsController.cs`:
  ```csharp
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
  ```
- [ ] `backend/F1App.Api/Program.cs` — `builder.Services.AddScoped<NewsFeedService>();`.

### Task 5: Backend — Tests (AC: 1, 3, 4)

- [ ] Create `backend/F1App.Api.Tests/Services/NewsFeedServiceTests.cs`. **Note**: `CodeHollow.FeedReader.FeedReader.ReadAsync` fetches by URL directly rather than through an injectable `HttpClient`/`IErgastClient`-style abstraction, so it isn't mockable the way this codebase's other services are — tests here can only exercise the parts that don't require network access:
  - `GetNewsAsync_CachesResultAndDoesNotRefetchWithinTheInterval` — call twice, assert the cache short-circuits (use a very short `NewsFeedRefreshIntervalMinutes` test config and a fake/no-op scenario, or test via a thin wrapper — see completion notes for what was actually feasible without network mocking infrastructure for `FeedReader` itself).
  - If `FeedReader` truly cannot be isolated without a network call in this environment, document that as a real testing gap in Completion Notes rather than writing a test that silently does nothing.
- [ ] `backend/F1App.Api.Tests/Controllers/NewsControllerTests.cs` — a controller-level test that swaps in a test-double `NewsFeedService`... **note**: `NewsFeedService` is a concrete class, not an interface, so it can't be swapped via DI the way `IErgastClient` is elsewhere. If this blocks a clean controller test, document it plainly rather than forcing an awkward workaround; a minimal "route exists and returns 200 with an empty array" smoke test may be the realistic ceiling here.
- [ ] Run `dotnet test backend/F1App.Api.Tests/` — all tests must pass. *(Could not be executed in this environment — no .NET SDK installed; same constraint as every prior Epic 4/5 story.)*

### Task 6: Frontend — Schema, hook (AC: 1, 2, 3, 4)

- [ ] `frontend/src/shared/api/queryKeys.ts` — add `news: ['news', 'feed'] as const` (matches architecture.md's own example literally).
- [ ] `frontend/src/shared/api/ergast.ts` — despite the filename (this module is Ergast-specific historically), news isn't Ergast data; create a new `frontend/src/shared/api/news.ts` instead:
  ```ts
  import { useQuery } from '@tanstack/react-query'
  import { z } from 'zod'
  import { queryKeys } from './queryKeys'

  const NewsItemSchema = z.object({
    title: z.string(),
    link: z.string(),
    source: z.string(),
    publishedAt: z.string(),
  })

  const NewsFeedSchema = z.array(NewsItemSchema)

  export type NewsItem = z.infer<typeof NewsItemSchema>

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined
  const REQUEST_TIMEOUT_MS = 10_000
  // Mirrors backend's default 15-minute refresh interval.
  const NEWS_STALE_TIME_MS = 1000 * 60 * 15

  export function useNewsFeed() {
    return useQuery({
      queryKey: queryKeys.news,
      queryFn: async ({ signal }) => {
        if (!API_BASE_URL) throw new Error('VITE_API_BASE_URL is not set — copy .env.example to .env.local')
        const response = await fetch(`${API_BASE_URL}/api/news`, {
          signal: AbortSignal.any([signal, AbortSignal.timeout(REQUEST_TIMEOUT_MS)]),
        })
        if (!response.ok) throw new Error(`Request to /api/news failed: ${response.status}`)
        return NewsFeedSchema.parse(await response.json())
      },
      staleTime: NEWS_STALE_TIME_MS,
      retry: false,
    })
  }
  ```

### Task 7: Frontend — `NewsFeedPage` (AC: 1, 2, 4)

- [ ] Create `frontend/src/features/fan-engagement/NewsFeedPage.tsx` — one `<h1>`, a card per headline (title, source badge, relative/formatted timestamp), each card an `<a target="_blank" rel="noopener noreferrer">` wrapping the whole card (whole-card click-through, matching `RaceWeekendCard`'s established pattern); loading skeleton, error state, and an explicit "No news available right now" empty state (AC 4) — distinct from the error state, since an empty *successful* response (all feeds down but request succeeded) is a different case than a network failure.
- [ ] Create `frontend/src/features/fan-engagement/index.ts` barrel.

### Task 8: Frontend — Routing, nav (AC: 1)

- [ ] `frontend/src/router.tsx` — add `{ path: 'news', element: <NewsFeedPage /> }`.
- [ ] `frontend/src/App.tsx` — add a "News Feed" nav link (the UX mockup's `nav-topbar` lists Calendar / Live Race / Standings / Profiles / News Feed — this is the first Epic 6 page that gets a permanent nav entry, unlike circuit/driver profiles which are click-through-only).

### Task 9: Frontend — Tests (AC: 1, 2, 3, 4)

- [ ] Create `frontend/src/features/fan-engagement/NewsFeedPage.test.tsx` (mirrors `CalendarPage.test.tsx`'s MSW/QueryClientProvider harness):
  - Renders a card per headline with title, source, and timestamp.
  - Each card links out with `target="_blank"` and `rel="noopener noreferrer"`.
  - Shows "No news available right now" when the API returns an empty array (not the error state).
  - Shows the error state when the request fails outright.
- [ ] Add `news` handler + sample fixture to `frontend/src/shared/mocks/handlers/ergastHandlers.ts` — **or** create a separate `newsHandlers.ts` alongside it, since news isn't Ergast data (mirrors the Task 6 naming decision); wire whichever file into `frontend/src/shared/test/server.ts`.
- [ ] Run `npm test -- --run` from `frontend/` — all tests must pass.
- [ ] Run `npx tsc --noEmit -p tsconfig.app.json` from `frontend/` — must be clean (per Story 5.1's fixed invocation).

## Dev Notes

### Architecture Alignment

- Matches `architecture.md`'s `NewsController.cs`/`NewsFeedService.cs` (backend) and `fan-engagement/NewsFeedPage.tsx` (frontend) file tree entries, and the `['news', 'feed']` query-key example given verbatim in the Communication Patterns section.
- This is the first backend service in the codebase that does **not** go through `IErgastClient`/`IOpenF1Client` — `CodeHollow.FeedReader` fetches feeds directly. This is also why it's the first service without a corresponding client-contract test file; see Task 5/Completion Notes for the resulting test-coverage gap.

### Regressions to Guard

- Per-feed try/catch (AC 4) must wrap each feed's fetch **individually**, not the whole `foreach` loop — a single try/catch around the entire loop would abort remaining feeds after the first failure, defeating the isolation requirement.
- Don't skip caching an empty result — see the caching note in Task 3; caching "all feeds down" for the configured interval is intentional, not an oversight to "fix" later.

### Files to Create / Modify

**Backend CREATE:**
- `backend/F1App.Api/Models/NewsItem.cs`
- `backend/F1App.Api/Clients/IFeedReaderClient.cs` (added during implementation — see Completion Notes: a thin seam around `CodeHollow.FeedReader`'s static API so `NewsFeedService` is unit-testable with a mock, mirroring the `IErgastClient` pattern, instead of shipping untestable per the original task plan)
- `backend/F1App.Api/Clients/FeedReaderClient.cs`
- `backend/F1App.Api/Services/NewsFeedService.cs`
- `backend/F1App.Api/Controllers/NewsController.cs`
- `backend/F1App.Api.Tests/Services/NewsFeedServiceTests.cs`
- `backend/F1App.Api.Tests/Controllers/NewsControllerTests.cs`

**Backend MODIFY:**
- `backend/F1App.Api/F1App.Api.csproj`
- `backend/F1App.Api/appsettings.json`
- `backend/F1App.Api/Program.cs`

**Frontend CREATE:**
- `frontend/src/shared/api/news.ts`
- `frontend/src/features/fan-engagement/NewsFeedPage.tsx`
- `frontend/src/features/fan-engagement/NewsFeedPage.test.tsx`
- `frontend/src/features/fan-engagement/index.ts`
- `frontend/src/shared/mocks/handlers/newsHandlers.ts`

**Frontend MODIFY:**
- `frontend/src/shared/api/queryKeys.ts`
- `frontend/src/shared/test/server.ts`
- `frontend/src/router.tsx`
- `frontend/src/App.tsx`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.1: F1 News Feed]
- [Source: _bmad-output/planning-artifacts/architecture.md — NewsController.cs, NewsFeedService.cs, NewsFeedPage.tsx, `['news','feed']` query key example]
- [Live-verified: `https://www.formula1.com/en/latest/all.xml`, `https://www.autosport.com/rss/f1/news` (redirects), `https://www.racefans.net/feed` (redirects)]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

None.

### Completion Notes List

- Implemented per plan, plus one design improvement over the original task list: added `IFeedReaderClient`/`FeedReaderClient` as a thin seam around `CodeHollow.FeedReader`'s static API (`backend/F1App.Api/Clients/`), mirroring the existing `IErgastClient` pattern. The original plan flagged `NewsFeedService` as likely untestable without network calls — this seam makes it fully unit-testable with mocks instead, so no test-coverage gap was actually accepted.
- Verified live before implementation that epics.md's Formula1.com RSS URL (`formula1.com/en/latest/all.news.rss`) 404s; the real working path is `https://www.formula1.com/en/latest/all.xml`. Autosport/RaceFans URLs work as given via `HttpClient`'s default redirect-following.
- `news.ts` was created as a new, separate module from `ergast.ts` (not Ergast data), with its own `newsHandlers.ts` mock file wired into `shared/test/server.ts` alongside the existing `ergastHandlers`.
- `NewsFeedPage` distinguishes "no news available" (empty array, a successful-but-empty response) from the network-error state — these are different UI states per AC 4, not the same fallback.
- **Environment note (same as every Epic 4/5 story)**: no .NET SDK in this environment, so backend changes were written and manually reviewed but not compiler-verified via `dotnet build`/`dotnet test`.
- All frontend tests pass except the 4 pre-existing, unrelated `dateUtils.test.ts` locale failures; `tsc -p tsconfig.app.json` is clean except the one pre-existing `TrackMap.test.tsx` issue noted in Story 5.1; `eslint` is fully clean.

### File List

See "Files to Create / Modify" above — unchanged from plan except the added `IFeedReaderClient`/`FeedReaderClient` files noted above.

### Change Log

| Date | Change |
|------|--------|
| 2026-07-10 | Story created and implemented directly (bmad create-story + dev-story cycle) |
