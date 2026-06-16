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

const MS_PER_DAY = 24 * 60 * 60 * 1000

export function formatRaceTime(iso: string): string {
  return RACE_TIME_FORMATTER.format(new Date(iso))
}

export function formatSessionTime(iso: string): string {
  return SESSION_TIME_FORMATTER.format(new Date(iso))
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
