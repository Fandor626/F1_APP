const RACE_DATE_FORMATTER = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
})

const MS_PER_DAY = 24 * 60 * 60 * 1000

export function formatRaceStart(iso: string): string {
  return RACE_DATE_FORMATTER.format(new Date(iso))
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
