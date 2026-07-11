---
name: F1_poc Phase 2
status: final
sources:
  - _bmad-output/phase-1/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/DESIGN.md
  - _bmad-output/phase-1/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/EXPERIENCE.md
  - _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/prd.md
  - _bmad-output/phase-2/planning-artifacts/prds/prd-F1_poc-2026-07-11/addendum.md
updated: 2026-07-11
---

# F1_poc Phase 2 — Experience Spine

Inherits from `_bmad-output/phase-1/planning-artifacts/ux-designs/ux-F1_poc-2026-06-16/` — this document specifies only what Phase 2 adds or changes. Every phase-1 IA row, voice rule, component, state, and flow not mentioned below still applies unchanged. Both phase-1 and this document win on conflict with any mock; this document wins on conflict with phase-1 where the two genuinely differ (see Foundation and Accessibility Floor below).

## Foundation

Unchanged: single-surface responsive web, no native app, no auth, fully hand-built components (no headless library). One real shift: the PRD raises the accessibility bar from phase-1's "proportionate floor, not a formal WCAG AA certification target" to an explicit target — "all Phase 2 surfaces target WCAG 2.1 AA" with an automated-audit success metric (Lighthouse/axe ≥ 95 on Calendar, Live Race, Standings, Fan Card, Race Weekend Detail). See Accessibility Floor below for what that changes in practice.

## Information Architecture

Delta only — surfaces and rows not listed here are unchanged from phase-1's IA table.

| Surface | What's new in Phase 2 | Realizes |
|---|---|---|
| Race Calendar | Default view is upcoming-only (no past weekends); a Future/Past/All filter control is added; a persistent Championship Sidebar (Drivers + Constructors standings) sits alongside the race list; Race Weekend cards are redesigned (track outline panel, two-line fastest-lap block, no per-card standings) | FR-1–FR-4 |
| Live Race | When no session is live, the page is guaranteed non-empty (hardens existing fallback) and gains a fixed-bottom Race Replay control bar (play/pause, snap-to-lap scrub, 1x/2x/4x speed, restart) | FR-5–FR-9 |
| Standings | Gains a Fan Card creation prompt (modal) for users with zero Fan Cards | FR-10 |
| Fan Card | Visual redesign (driver photo, autograph, team logo, team-principal name); a user can hold and view multiple cards, shown as a grid | FR-11, FR-12 |
| Race Weekend Detail | Previously spine-only (no mock) in phase-1; now has a rendered composition. Gains: larger/detailed track layout, a separate Track Records section, a Circuit History section (stat tiles + past-winners list), and a plain-language Win Prediction callout with the MVP's raw win-probability table demoted behind a toggle | FR-13–FR-16 |
| Circuit / Driver / Constructor Profile | Career stats now presented in visually distinct groups (season-by-season vs. career totals, head-to-head where applicable) instead of one flat list | FR-17 |
| News Feed | Each item gains a thumbnail and a one-line snippet | FR-18 |

→ Composition reference: `mockups/key-calendar.html`, `mockups/key-live-race.html`, `mockups/key-fancard.html`, `mockups/key-race-weekend-detail.html`. Standings (prompt only), Circuit/Driver/Constructor Profile, and News Feed remain spine-only for Phase 2 — no dedicated mock was rendered for them (the Fan Card mock illustrates the Standings-page prompt as a secondary composition). Spine wins on conflict with any mock.

## Voice and Tone

Addition to phase-1's table — same register (plain, fan-to-fan, no hype/emoji), applied to two new content types:

| Do | Don't |
|---|---|
| "Most likely to win: Max Verstappen. He's started from pole here two of the last three years, and Red Bull's long-run pace has topped every practice session this weekend." | "78% WIN PROBABILITY 🏆" or any raw-percentage headline claim |
| Fan Cards stay plain artifacts — driver name, team, team principal, autograph | Rarity labels, collectible/badge language, "legendary" or tier framing (explicit Non-Goal, PRD §5) |

## Component Patterns

Behavioral. Visual specs live in `DESIGN.md.Components`.

| Component | Use | Behavioral rules |
|---|---|---|
| Calendar filter (`tab-toggle`) | Calendar | Three-way switch: All / Future / Past. Future is the default and initial state on every load. Switching is instant, no route change — mirrors the existing Standings Drivers/Constructors toggle behavior. |
| Championship Sidebar | Calendar | Read-only; same live data source as Standings. Stays reachable regardless of filter selection or race-list scroll position. Collapses to a tappable drawer on mobile, expanding to the same two standings groups on tap. |
| Race Weekend card v2 | Calendar | Whole card still clickable → detail view (unchanged from phase-1). No longer carries championship data — that's the sidebar's job now. Missing historical lap data for a circuit omits the fastest-lap block gracefully rather than erroring. |
| Race Replay bar | Live Race (fallback/replay mode only) | Visible only when no session is live. Play/pause toggles playback; scrub bar jumps to a specific lap and updates every dependent view (positions, gaps, tyres), not just a timeline marker; speed changes apply immediately without a restart; Restart returns to lap 1 without a page reload; pause/resume continues from the exact paused lap. The lap readout (`Lap {n} / {total}`) updates live during both playback and scrub-drag — it always reflects the scrub thumb's current position, never lags behind it. |
| Fan Card creation prompt | Standings | Modal, one-level-deep (existing overlay pattern). Launches directly into the existing 3-step wizard. Does not appear for users who already hold ≥1 Fan Card. Dismissing doesn't block further use of the page. |
| Trading card + grid | Fan Card | Each card shows driver photo, autograph, team logo, team-principal name, plus everything the MVP card showed. Creating a new card never overwrites an existing one; all cards are visible in the grid at once (no carousel/pagination). An "Add new card" tile re-enters the wizard. |
| Win Prediction callout | Race Weekend Detail | Names a driver and a short plain-language reason. No percentages visible by default. A toggle button expands/collapses the original per-grid-slot probability table beneath it — collapsed by default. |
| Track Records section | Race Weekend Detail | All-time and current/most-recent-year fastest lap, each with a driver name — its own section, not merged into the track-layout card. |
| Circuit History section | Race Weekend Detail | Stat tiles (length, corners, DRS zones, first F1 race year) plus a past-winners list (year, driver, team); driver names link to their profiles. Circuits lacking full historical data show partial content, not an error. |
| Profile grouped-stats presentation | Driver / Constructor Profile | Career stats render as ≥2 visually distinct stacked sections (e.g., season-by-season, career totals, head-to-head where applicable) on one scrollable page — not tabs. No group requires horizontal scroll at 360px width. |
| News preview row | News Feed | Thumbnail left, one-line snippet, list-row layout. Items whose source lacks an image or snippet degrade to title-only (unchanged click-through behavior). |

## State Patterns

Addition to phase-1's table.

| State | Surface | Treatment |
|---|---|---|
| Filter = Future (default) | Calendar | Zero past Race Weekends rendered; next weekend still visually pinned. |
| Filter = Past / All | Calendar | Past shows only completed weekends; All shows the full season, unfiltered. |
| Fallback / replay mode | Live Race | Same components as a live session (gap list, chart, sector board, timeline), populated from the last completed race, labelled "Past race — {name}, {date}" (inherited phase-1 pattern) plus the new Replay bar. |
| Zero completed races this season | Live Race | No Replay bar (nothing to replay). Plain on-brand message in place of the race components — e.g. "No races completed yet this season — first race: {next race name}, {date}." — never a blank page, consistent with phase-1's "First race at this circuit." pattern. |
| Replay playing / paused / scrubbing | Live Race | Playing advances lap-by-lap at the selected speed; paused holds at the current lap and resumes from exactly that point; scrubbing jumps immediately, no animation catch-up. Backgrounding the tab mid-replay and returning resumes paused at the same lap — within the same page session only, no cross-reload persistence `[inherits PRD assumption]`. |
| Fan Card prompt shown / dismissed | Standings | Shown once per browser for users with zero cards; dismissal is remembered for some period rather than reappearing every visit. `[NOTE FOR UX] exact suppression window was not decided this session — PRD flags it as an open UX-stage decision (§10) and it remains open here.` |
| Zero Fan Cards vs. N Fan Cards | Fan Card page | Zero cards: the page leads with the "Add new card" tile as the primary/only tile (empty-state framing, no "locked feature" language). N cards: grid shows all N plus the add tile. |
| Win Prediction table collapsed / expanded | Race Weekend Detail | Collapsed by default (plain-language callout only); expanding reveals the unchanged MVP probability table beneath a dashed divider. |
| No qualifying yet | Race Weekend Detail | Win Prediction callout is simply absent (same "absent, not placeholder" rule phase-1 used for the old probability widget). |
| No prior result / incomplete circuit history | Race Weekend Detail | Track Records and Circuit History sections show whatever data exists (e.g., stat tiles without a winners list) rather than an error state. |

## Interaction Primitives

Addition to phase-1's table.

- Calendar filter is a standard `tablist`/`tab` pattern (arrow-key or tab navigation between the three options), consistent with the existing Standings toggle.
- Replay scrub bar is operable by drag and is keyboard-reachable (`Tab` to focus, `role="slider"`). Left/Right arrow keys step one lap at a time, matching the snap-to-lap tick behavior; `Home`/`End` jump to lap 1 and the final lap (standard slider convention, not separately elicited but low-risk to default).
- Speed button group and Restart are standard button activation (`Enter`/`Space`, click/tap).
- Fan Card "Add new card" tile behaves like any other modal-launching control (`Enter`/`Space` opens the wizard).
- Win Prediction toggle uses `aria-expanded`/`aria-controls` and standard button activation.

## Accessibility Floor

Behavioral; visual contrast lives in `DESIGN.md`.

- Phase 2 turns phase-1's informal floor into a hard requirement (PRD §7, §8 SM-2): the audit thresholds named in Foundation are now a success metric, not just a target — a real tightening, not a restatement.
- Track outline visuals (Calendar card, Race Weekend Detail) get a screen-reader-appropriate label naming the circuit — the outline itself is decorative/non-informational to a non-sighted user, so the accessible name (e.g., "Track layout: Circuit de Spa-Francorchamps") stands in for it, not a literal shape description.
- The Championship Sidebar is a labelled landmark region (e.g., `aria-label="Championship standings"`) so it's independently reachable/skippable.
- All new interactive surfaces (Calendar filter, Replay controls, Win Prediction toggle, Fan Card prompt/wizard entry) are keyboard-operable with a visible focus ring, per the inherited phase-1 requirement — still mandatory given the fully custom component base.
- Replay scrub bar's keyboard contract (see Interaction Primitives) is fully defined — arrow-key stepping plus Home/End — so it meets the AA target this phase commits to rather than being a focusable-but-undefined slider.

## Responsive & Platform

Addition to phase-1's table.

| Breakpoint | Behavior |
|---|---|
| Desktop (`≥ md`) | Championship Sidebar renders as a sticky left rail. Replay bar spans the full viewport width, with all five controls (play/pause, lap readout, scrub, restart, speed group) inline. Fan Card grid shows multiple columns. |
| Mobile (`< md`) | Championship Sidebar collapses to a drawer summary above the race list. Replay bar collapses to two controls only — play/pause and the scrub bar — kept within one-thumb reach per the PRD's mobile NFR; Restart and the speed group move behind a compact overflow (`⋯`) button rather than staying inline. Fan Card grid reflows to fewer columns (single column at narrow widths). |

## Key Flows

### Flow 1 — Wednesday check-in (Priya, returning fan, mid-week, no session live) — realizes UJ-1

1. Priya opens the app out of habit and lands on Calendar — still the default page.
2. The page is already filtered to Future: no past races to scroll past. The next weekend, Belgian GP, is pinned at the top with its track outline and both fastest-lap lines visible.
3. The Championship Sidebar sits to the left, showing Norris leading the Drivers' standings and McLaren leading Constructors' — she doesn't need to visit Standings separately just to check this.
4. **Climax:** Everything she came for — what's next, who's winning — is visible in the first screen, no scrolling, no filtering out stale cards in her head.
5. She clicks the Belgian GP card to open the detail view, curious about the win prediction.

Failure: she flips the filter to Past out of curiosity — the list swaps to completed weekends only, the sidebar doesn't move or reset.

### Flow 2 — Tuesday afternoon replay (Tom, casual fan, bookmarked Live Race, nothing currently live) — realizes UJ-2

1. Tom opens his Live Race bookmark directly, mid-week.
2. Instead of an empty page, the British Grand Prix — the most recently completed race — loads fully: gap list, sector board, track map, all labelled "Past race — British Grand Prix, 6 Jul 2026."
3. The Replay bar is docked at the bottom. He hits play; the gap list starts advancing lap by lap, exactly like a live session would.
4. He drags the scrub bar to the final laps to relive the finish, then bumps speed to 2x to watch the last stint.
5. **Climax:** He pauses right as Verstappen closes to within a second of Norris — the gap cell highlights just like it would have live — and just sits with that moment instead of reading a static results table.
6. He backgrounds the tab to check something else; when he comes back, playback is still paused at that exact lap.

Failure: no completed race exists yet this season (extremely early season) — see the "Zero completed races this season" row in State Patterns: no Replay bar, a plain on-brand message names the next race instead of a blank page.

### Flow 3 — Two cards (Aisha, a couple of visits in, never noticed Fan Card) — realizes UJ-3

1. Aisha is on Standings checking the constructors' table when a modal appears: "Build your Fan Card."
2. She clicks through the existing 3-step wizard — driver, constructor, circuit — and lands back on the Fan Card page.
3. Her new card renders: Lando Norris's photo panel, McLaren's team rule and badge, "Team Principal: Andrea Stella," and a script-styled autograph.
4. **Climax:** It looks good enough that she immediately clicks the "Add new card" tile again and makes a second one for Verstappen — the grid now shows both cards side by side, no overwrite, no account needed.
5. She leaves the tab open; both cards are still there next time she opens the app on the same browser.

Failure: she dismisses the modal instead ("Not now") — Standings keeps working normally, and the prompt doesn't reappear on her next few visits.

### Flow 4 — The night before qualifying (Marcus, history-minded fan, evening before Spa quali) — realizes UJ-4

1. Marcus opens the Belgian GP's Race Weekend Detail page from the Calendar.
2. He sees the real Spa-Francorchamps outline, larger and more detailed than the calendar card's version, with the sessions list already in Local Time.
3. Track Records shows Bottas's all-time lap and Leclerc's 2026 best, each with a driver name he can click.
4. Circuit History gives him length, corner count, DRS zones, and first-race year, plus the last three winners at Spa.
5. **Climax:** The Win Prediction callout reads plainly: "Most likely to win: Max Verstappen — pole here two of the last three years, Red Bull's long-run pace topped every practice session this weekend." He didn't have to parse a probability table to get the gist — though it's one click away if he wants it.
6. Curious, he clicks through to Verstappen's driver profile and finds the same grouped, scannable presentation (season-by-season, career totals) rather than a jarring drop into a different page style.

Failure: it's a circuit with no qualifying session run yet — the Win Prediction callout is simply absent, not a placeholder or error.

### Flow 5 — Deciding what's worth a click (Elena, between races, skimming News Feed) — realizes UJ-5

1. Elena opens News Feed, same entry point as always.
2. Each headline now carries a small thumbnail and a one-line snippet instead of a bare title.
3. **Climax:** She can tell from the thumbnail and snippet alone that one story is about a driver contract rumor she doesn't care about, and skips it — but a second story's thumbnail and snippet make clear it's about a technical directive that affects her favorite team, so she clicks through.
4. Clicking redirects her to the source site, unchanged from the MVP.

Failure: one source's RSS feed doesn't populate a thumbnail or snippet — that item falls back to title-only, and it doesn't block the rest of the feed from rendering normally.
