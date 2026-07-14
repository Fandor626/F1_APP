# Driver photo assets

Hand-curated static assets (AD-10) — not fetched from any API.

Naming convention: `{driverId}.jpg`, where `driverId` matches the Ergast-style
id already used throughout the app (e.g. `norris`, `max_verstappen`,
`leclerc`).

No photo files are checked in yet. `FanCard.tsx` falls back to an initials
placeholder whenever a driver's file is missing, so the feature works fully
without any files present here — add `.jpg` files here as licensed assets
become available.
