import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

// AD-12 / PRD SM-2: the five pages named by the success metric. "Race Weekend
// Detail" needs a concrete round — round 1 is the season's first race and is
// always resolvable against the live current-season schedule.
const PAGES: { name: string; path: string }[] = [
  { name: 'Calendar', path: '/' },
  { name: 'Live Race', path: '/live' },
  { name: 'Standings', path: '/standings' },
  { name: 'Fan Card', path: '/fan-card' },
  { name: 'Race Weekend Detail', path: '/races/1' },
]

for (const { name, path } of PAGES) {
  test(`${name} page has no critical or serious accessibility violations`, async ({ page }) => {
    await page.goto(path)
    await page.waitForLoadState('networkidle')

    const results = await new AxeBuilder({ page }).analyze()

    // Axe doesn't produce a Lighthouse-style 0-100 score — the PRD's "≥95"
    // success metric is enforced here as its violation-blocking equivalent:
    // zero critical/serious findings. Non-blocking (moderate/minor) findings
    // are still visible in the report for follow-up but don't fail the build.
    const blocking = results.violations.filter((v) => v.impact === 'critical' || v.impact === 'serious')

    expect(blocking, JSON.stringify(blocking, null, 2)).toEqual([])
  })
}
