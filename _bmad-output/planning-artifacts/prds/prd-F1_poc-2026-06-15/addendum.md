# PRD Addendum — F1 Fan Web Application

_Technical decisions and implementation context that belong downstream (architecture, solution design) rather than in the PRD itself._

---

## Stack Decisions

- **Frontend:** React (SPA)
- **Backend:** C# (ASP.NET Core) — acts as API proxy/aggregator, RSS fetcher, and win probability calculator
- **Primary data source (historical):** Ergast API — free, no auth, REST, covers 1950–present
- **Primary data source (live):** OpenF1 API — free, no auth, REST + streaming, covers 2023–present
- **Local persistence:** Browser localStorage (no backend DB required for POC)

## Deployment Options Considered (Post-POC)

| Service | Frontend | Backend | Free Tier |
|---------|----------|---------|-----------|
| Vercel + Render | React | C# (Docker) | Yes |
| Netlify + Railway | React | C# | Yes (Railway has $5 credit/mo) |
| Azure Static Web Apps + Azure App Service | React | C# | Limited free tier |

Recommended path: Vercel (frontend) + Render (backend Docker container).

## API Notes

- **Ergast API**: Deprecated notice exists — data continues to be served but no new features. Ergast data mirrors are available. Worth noting for architecture: consider a caching layer to reduce dependency on Ergast availability.
- **OpenF1 API**: Active project, WebSocket and REST endpoints. Car position data available at ~3.7Hz during sessions.
- **RSS feeds identified**: Formula1.com/en/latest/all.news.rss, autosport.com/rss/f1/news, racefans.net/feed

## Post-POC Auth Notes

Features that will move from local-storage to account-backed on auth introduction:
- Race weekend streak (FR-24)
- My F1 fan card (FR-25)
- Push notifications (non-goal for POC)
- Race prediction game (non-goal for POC)
