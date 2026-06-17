const RACE_TIME_FORMATTER = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  hour: '2-digit',
  minute: '2-digit',
})

const SESSION_TIME_FORMATTER = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
})

// timeZone:'UTC' here is intentional — the caller pre-shifts the timestamp by
// the circuit's UTC offset, so displaying at UTC shows the circuit wall-clock time.
const TRACK_TIME_FORMATTER = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'UTC',
})

const MS_PER_DAY = 24 * 60 * 60 * 1000

export function formatRaceTime(iso: string): string {
  return RACE_TIME_FORMATTER.format(new Date(iso))
}

export function formatSessionTime(iso: string): string {
  return SESSION_TIME_FORMATTER.format(new Date(iso))
}

function parseOffsetMinutes(iso: string): number {
  const m = iso.match(/([+-])(\d{2}):(\d{2})$/)
  if (!m) return 0
  const sign = m[1] === '+' ? 1 : -1
  return sign * (Number(m[2]) * 60 + Number(m[3]))
}

export function formatSessionTimeForMode(iso: string, mode: 'local' | 'track'): string {
  const date = new Date(iso)
  if (mode === 'local') return SESSION_TIME_FORMATTER.format(date)
  const offsetMs = parseOffsetMinutes(iso) * 60_000
  return TRACK_TIME_FORMATTER.format(new Date(date.getTime() + offsetMs))
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

export function describeDaysUntil(raceStart: Date, now: Date): string {
  const days = Math.round((startOfDay(raceStart).getTime() - startOfDay(now).getTime()) / MS_PER_DAY)
  if (days <= 0) return 'today'
  if (days === 1) return 'tomorrow'
  return `${days} days`
}

const RANGE_DAY_FORMATTER = new Intl.DateTimeFormat(undefined, { day: 'numeric' })
const RANGE_MONTH_FORMATTER = new Intl.DateTimeFormat(undefined, { month: 'short' })

export function formatDateRange(start: Date, end: Date): string {
  const year = end.getFullYear()

  if (start.toDateString() === end.toDateString()) {
    return `${RANGE_DAY_FORMATTER.format(end)} ${RANGE_MONTH_FORMATTER.format(end)} ${year}`
  }

  const sameMonth = start.getFullYear() === end.getFullYear() && start.getMonth() === end.getMonth()
  if (sameMonth) {
    return `${RANGE_DAY_FORMATTER.format(start)}–${RANGE_DAY_FORMATTER.format(end)} ${RANGE_MONTH_FORMATTER.format(end)} ${year}`
  }

  return `${RANGE_DAY_FORMATTER.format(start)} ${RANGE_MONTH_FORMATTER.format(start)} – ${RANGE_DAY_FORMATTER.format(end)} ${RANGE_MONTH_FORMATTER.format(end)} ${year}`
}
