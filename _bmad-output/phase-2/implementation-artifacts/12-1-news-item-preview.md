---
baseline_commit: a01d4ff
---

# Story 12.1: News item preview

Status: review

## Story

As a fan skimming the News Feed,
I want to see a photo and short snippet for each headline,
so that I can judge relevance before clicking through to the source site.

## Acceptance Criteria

1. **Given** a news item whose source feed provides an enclosure image and description, **when** it renders in the News Feed, **then** it shows a thumbnail (left, list-row style) and a one-line snippet, alongside the existing title, source, and publish time.
2. **Given** the backend feed-parsing pipeline (`NewsFeedService`, `CodeHollow.FeedReader`), **when** it processes each feed item, **then** it extracts `imageUrl` (from the item's enclosure, if present) and `snippet` (from the item's Description, truncated) at parse time onto the existing `Models/NewsItem.cs` — no new external data source or second network hop (AD-11).
3. **Given** a news item whose source lacks an image or snippet, **when** it renders, **then** it degrades gracefully to title-only, not a broken-image placeholder.
4. **Given** a news item, **when** I click it, **then** it still redirects to the original source article, unchanged from the MVP.

## Tasks / Subtasks

- [x] Task 1: Extract image + description in the feed-parsing pipeline (AC 2)
  - [x] `Clients/IFeedReaderClient.cs`: `FeedReaderItem` gains `ImageUrl` and `Description` (both `string?`, optional/defaulted so no existing call site breaks).
  - [x] `Clients/FeedReaderClient.cs`: new `ExtractImageUrl(FeedItem)` reads the format-specific `SpecificItem` — `Rss20FeedItem`/`Rss092FeedItem`'s `<enclosure>` when its `MediaType` starts with `image/`, with a `MediaRssFeedItem` fallback (first `media:thumbnail`, then an image-typed enclosure). Verified against the real `CodeHollow.FeedReader` 1.2.6 API via reflection (no XML doc/source browsing available) before writing this, since none of it is exercised by any existing test in this codebase. `Description` passes through from the common `FeedItem.Description` unmodified — no extraction needed there, it's already flat text/HTML on the common type.
- [x] Task 2: `NewsItem` model + snippet building (AC 2, 3)
  - [x] `Models/NewsItem.cs`: gains `ImageUrl` and `Snippet` (both `string?`, defaulted to `null` so the one existing positional-construction call site in `NewsFeedService` — and any test — doesn't need updating just to keep compiling).
  - [x] `Services/NewsFeedService.cs`: new `BuildSnippet(string? description)` — strips HTML tags (regex), decodes entities (`WebUtility.HtmlDecode`), collapses whitespace, and truncates to 140 chars at a word boundary with a trailing `…`. Returns `null` (not an empty string) for null/blank/tag-only input — this is what AC 3's "degrades gracefully" maps to on the backend side; the frontend never sees an empty snippet to render as a blank line.
- [x] Task 3: Frontend thumbnail + snippet rendering (AC 1, 3, 4)
  - [x] `frontend/src/shared/api/news.ts`: `NewsItemSchema` gains `imageUrl`/`snippet`, both `.optional()` (matching the backend's `JsonIgnoreCondition.WhenWritingNull` — absent key, not `null`, when missing).
  - [x] `frontend/src/features/fan-engagement/NewsFeedPage.tsx`: extracted a `NewsCard` subcomponent (was inline `<li><a>...` markup) so each card can own its own `imageFailed` state — thumbnail `<img onError={() => setImageFailed(true)}>` on the left (list-row style, `56×56px` `object-cover`), swapping to no image at all (not a broken-image icon) on load failure, matching the same `onError` fallback pattern already used by `FanCard.tsx`/`NewsFeedPage`'s sibling features. Snippet renders as a `truncate`d one-line `<p>` only when present; both are entirely absent (not empty placeholders) when the source lacks them. `href`/`target="_blank"`/`rel="noopener noreferrer"`/`data-testid="news-card"` all unchanged (AC 4).
- [x] Task 4: Tests
  - [x] Backend: `NewsFeedServiceTests` — two new tests (`imageUrl`/`snippet` map through from a feed item that has them; both degrade to `null` when absent) plus `[Theory]`-driven `BuildSnippet` unit tests (HTML stripping, entity decoding, blank/null → `null`) and a truncation test (word-boundary cut, ellipsis, no double spaces). New `Clients/FeedReaderClientTests.cs` — the first test file for `FeedReaderClient`, using `CodeHollow.FeedReader.FeedReader.ReadFromString(...)` against real sample RSS XML (no network call) to exercise the actual enclosure-extraction logic end-to-end, since `NewsFeedServiceTests` only mocks at the `IFeedReaderClient` boundary and never touches this code path.
  - [x] Frontend: `NewsFeedPage.test.tsx` — new tests for thumbnail+snippet rendering, graceful degradation for the mock fixture's second item (which deliberately has neither field), `onError` fallback removing the `<img>` entirely, and an explicit unchanged-click-through assertion (`href` still points at the original source). Extended `sampleNewsFeed`'s first fixture item with `imageUrl`/`snippet`, left the rest without — giving every new test both a "has data" and a "missing data" fixture to assert against without needing per-test overrides.

## Dev Notes

- `CodeHollow.FeedReader`'s enclosure/thumbnail API is not previously used anywhere in this codebase and has no XML/HTML documentation bundled for browsing — verified the actual public surface (`FeedItem.SpecificItem`, `Rss20FeedItem.Enclosure`, `FeedItemEnclosure.{Url,MediaType}`, `MediaRssFeedItem.Media`/`.Enclosure`, `MediaRSS.Thumbnail.Url`) via .NET reflection against the installed package DLL before writing `ExtractImageUrl`, rather than guessing at property names.
- No HTML-parsing package exists in this project (confirmed via `.csproj` grep) — `BuildSnippet` deliberately uses only BCL (`System.Text.RegularExpressions.Regex` + `System.Net.WebUtility.HtmlDecode`) rather than adding a dependency (e.g. HtmlAgilityPack) for a single strip-and-truncate operation, consistent with the codebase's existing minimal-dependency footprint.
- Neither the epics.md AC nor UX-DR10 specifies exact thumbnail pixel dimensions or snippet character-count — both are explicitly flagged gaps in UX-DR10, left to implementation discretion; chose `56×56px` (fits comfortably within the existing `h-20` skeleton row height) and 140 characters (roughly one line at the existing `text-[12.5px]` size, truncated further by CSS `truncate` as a second line of defense against font/viewport variance).

### Architecture Compliance — AD-11 (verbatim)

> **AD-11 — News preview reuses the existing feed-parsing pipeline; no new fetch**
> - **Rule:** `NewsFeedService` (`CodeHollow.FeedReader`) extracts `imageUrl` (from the item's enclosure, if present) and `snippet` (from the item's `Description`, truncated) at parse time. The existing internal model, `Models/NewsItem.cs`, gains these two optional fields. Missing values degrade to title-only per FR-18's stated consequence — a per-item null, not an error.

### References

- [Source: _bmad-output/phase-2/planning-artifacts/epics.md#Story 12.1]
- [Source: _bmad-output/phase-2/planning-artifacts/architecture/architecture-F1_poc-2026-07-12/ARCHITECTURE-SPINE.md#AD-11]
- [Source: backend/F1App.Api/Services/NewsFeedService.cs], [Source: backend/F1App.Api/Clients/FeedReaderClient.cs]
- [Source: frontend/src/features/fan-engagement/FanCard.tsx] — `onError` broken-image fallback pattern reused for the news thumbnail

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Completion Notes List

- All 4 tasks complete. Full backend suite: 220/224 passing (11 net new tests, all passing) — the 4 failures are the same pre-existing, unrelated `CircuitProfileServiceTests`/`DriversControllerTests`/`StandingsControllerTests` issues confirmed across every story this epic. Full frontend suite: 204/208 passing — same pre-existing `dateUtils.test.ts` failures.
- `dotnet build` clean on both projects. `npx tsc -b` clean, `eslint` clean on all touched frontend files.
- Verified the real `CodeHollow.FeedReader` enclosure/`SpecificItem` API via .NET reflection against the installed package before implementing, rather than guessing — the library's public surface for this feature isn't exercised anywhere else in the codebase.

### File List

- Modified: `backend/F1App.Api/Clients/IFeedReaderClient.cs`
- Modified: `backend/F1App.Api/Clients/FeedReaderClient.cs`
- Modified: `backend/F1App.Api/Models/NewsItem.cs`
- Modified: `backend/F1App.Api/Services/NewsFeedService.cs`
- Modified: `backend/F1App.Api.Tests/Services/NewsFeedServiceTests.cs`
- New: `backend/F1App.Api.Tests/Clients/FeedReaderClientTests.cs`
- Modified: `frontend/src/shared/api/news.ts`
- Modified: `frontend/src/features/fan-engagement/NewsFeedPage.tsx`
- Modified: `frontend/src/features/fan-engagement/NewsFeedPage.test.tsx`
- Modified: `frontend/src/shared/mocks/handlers/newsHandlers.ts`
