# Phase 2 Intent: F1_poc UI/UX Pass

Phase 2 targets the F1_poc MVP (calendar, live race, standings, circuit/driver profiles, fan engagement), which is functionally complete but needs a UI/UX pass. Synthesis theme: **less clutter, always show something useful** — give the user something to DO, not just look at (replay controls, filters), and stop repeating generic data everywhere in favor of specific/textured data shown once in the right place (driver photo, track shape, real records).

## Must-have changes

### Calendar
- Default view hides past races; focuses on next race + upcoming races.
- Add a filter control: All / Future / Past races.
- Race cards stop repeating full championship standings (points, racers) on every card.
- Move championship standings out of race cards into a persistent sidebar shown once on the page.
- Sidebar scoreboard: full drivers list with current championship points.
- Sidebar also shows constructor/team standings list with points, alongside drivers.
- Race cards made visually bigger/more prominent.
- Race card shows the real circuit track outline/shape, not just text.
- Race card shows fastest lap stats at this circuit: all-time record and this year's/last year's fastest, with driver name.

### Live Race
- Fix: live race page currently shows an empty state when no race is in progress.
- Always show data: fall back to the last completed race's scoreboard/data when nothing is live.
- Add a replay/simulation control for the last race: start, stop, restart, rewind.
- Replay control: scrub bar, jump to any specific lap directly.
- Replay control: selectable playback speed (2x/4x etc.).
- Replay control: pause and resume/continue from wherever it was stopped.

### Standings
- Nudge users who haven't set up a Fan Card yet to go pick a favorite driver/team.
- Add a popup inviting users to create their Fan Card, rather than relying on them finding the page.

### Fan Card / Engagement
- Redesign: current Fan Card visual design doesn't look good.
- Include the driver's photo.
- Include the driver's autograph/signature.
- Include the team logo.
- Include team principal/CEO info (e.g. Toto Wolff for Mercedes).
- Allow users to create multiple Fan Cards (e.g. for 2-3 different drivers).

### Race Weekend Detail
- Data must be clear/informative, no unnecessary or confusing fields.
- Visually show the track layout, not just text.
- Show best lap at this track ever, and best lap of the current year, with driver name.
- Show track records and historical data about the circuit directly on the page.
- Show a simple, easy-to-understand win prediction (who can win and why) — avoid exposing complex/hard calculations to the user.

### Circuit / Driver Profiles
- Surface well-structured career stats and history: wins, which races won, places taken, etc.

## Parked for later (Could)
- Sidebar could show a live countdown to next race lights-out (days/hours/minutes).
- Standings page loading state: custom loader showing the current championship leader instead of a generic spinner.

## Scope decisions / boundaries
- Fan Cards stay non-gamified: clean personal card per driver, no rarity/collection mechanics.
