import { describe, expect, it } from 'vitest'
import { describeDaysUntil, formatDateRange } from './dateUtils'

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
