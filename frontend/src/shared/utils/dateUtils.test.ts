import { describe, expect, it } from 'vitest'
import { describeDaysUntil, formatDateRange, formatSessionTime, formatSessionTimeForMode } from './dateUtils'

// Renders in the runtime's default locale, so expectations are built from
// the same formatters rather than hardcoded English text.
const day = new Intl.DateTimeFormat(undefined, { day: 'numeric' })
const month = new Intl.DateTimeFormat(undefined, { month: 'short' })

describe('formatDateRange', () => {
  it('collapses to a single date when start and end are the same day', () => {
    const date = new Date('2026-09-07T13:00:00Z')

    expect(formatDateRange(date, date)).toBe(`${day.format(date)} ${month.format(date)} 2026`)
  })

  it('uses a compact range when both dates share a month', () => {
    const start = new Date('2026-09-05T00:00:00Z')
    const end = new Date('2026-09-07T13:00:00Z')

    expect(formatDateRange(start, end)).toBe(`${day.format(start)}–${day.format(end)} ${month.format(end)} 2026`)
  })

  it('repeats the month for each side when the range crosses a month boundary', () => {
    const start = new Date('2026-09-28T00:00:00Z')
    const end = new Date('2026-10-01T13:00:00Z')

    expect(formatDateRange(start, end)).toBe(
      `${day.format(start)} ${month.format(start)} – ${day.format(end)} ${month.format(end)} 2026`,
    )
  })
})

describe('formatSessionTimeForMode', () => {
  it('local mode returns the same result as formatSessionTime', () => {
    const iso = '2026-03-08T18:00:00+00:00'
    expect(formatSessionTimeForMode(iso, 'local')).toBe(formatSessionTime(iso))
  })

  it('track mode for +00:00 offset shows the wall-clock time from the ISO string', () => {
    expect(formatSessionTimeForMode('2026-03-08T18:00:00+00:00', 'track')).toContain('18:00')
  })

  it('track mode for +03:00 offset shows 15:30, not 12:30 UTC', () => {
    expect(formatSessionTimeForMode('2026-03-13T15:30:00+03:00', 'track')).toContain('15:30')
  })

  it('track mode for -05:00 offset shows 14:00, not 19:00 UTC', () => {
    expect(formatSessionTimeForMode('2026-03-20T14:00:00-05:00', 'track')).toContain('14:00')
  })

  it('track mode treats Z suffix as +00:00', () => {
    expect(formatSessionTimeForMode('2026-03-08T18:00:00Z', 'track')).toContain('18:00')
  })
})

describe('describeDaysUntil', () => {
  const now = new Date('2026-09-04T00:00:00Z')

  it('says "today" when the race is later the same day', () => {
    expect(describeDaysUntil(new Date('2026-09-04T18:00:00Z'), now)).toBe('today')
  })

  it('says "tomorrow" for the next calendar day', () => {
    expect(describeDaysUntil(new Date('2026-09-05T08:00:00Z'), now)).toBe('tomorrow')
  })

  it('counts whole calendar days regardless of time-of-day remainder', () => {
    expect(describeDaysUntil(new Date('2026-09-07T13:00:00Z'), now)).toBe('3 days')
  })
})
