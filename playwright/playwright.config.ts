import { defineConfig, devices } from '@playwright/test'

const FRONTEND_URL = process.env.E2E_BASE_URL ?? 'http://localhost:5173'
const BACKEND_URL = process.env.E2E_BACKEND_URL ?? 'http://localhost:5000'

// AD-12: accessibility assertions run against real running frontend + backend
// (not a mocked API layer) so the pages under test reflect actual rendered
// content — including Live Race's Story 8.1 fallback-enrichment data, which
// only exists when the backend can reach Ergast/OpenF1 for real.
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  timeout: 30_000,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? 'github' : 'list',
  use: {
    baseURL: FRONTEND_URL,
    trace: 'retain-on-failure',
  },
  webServer: process.env.E2E_SKIP_WEBSERVER
    ? undefined
    : [
        {
          command: 'dotnet run --project ../backend/F1App.Api/F1App.Api.csproj --urls http://localhost:5000',
          url: `${BACKEND_URL}/api/races`,
          timeout: 120_000,
          reuseExistingServer: !process.env.CI,
          env: {
            ASPNETCORE_ENVIRONMENT: 'Development',
            // Backend's real CORS config lives in the gitignored
            // appsettings.Development.json — this env-var override (ASP.NET
            // Core's `__` section-delimiter convention) gives CI the same
            // allow-list without checking in that file.
            AllowedOrigins__0: FRONTEND_URL,
          },
        },
        {
          command: 'npm run dev',
          cwd: '../frontend',
          url: FRONTEND_URL,
          timeout: 60_000,
          reuseExistingServer: !process.env.CI,
        },
      ],
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
})
