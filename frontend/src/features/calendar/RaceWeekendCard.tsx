import type { RaceWeekend } from '../../shared/api/ergast'
import { describeDaysUntil, formatRaceStart } from '../../shared/utils/dateUtils'

interface RaceWeekendCardProps {
  race: RaceWeekend
  isNext?: boolean
}

export function RaceWeekendCard({ race, isNext = false }: RaceWeekendCardProps) {
  const nextRaceLabel = `Next race: ${race.raceName}, ${describeDaysUntil(new Date(race.raceStart), new Date())}`

  return (
    <article
      data-testid="race-weekend-card"
      className={`rounded-lg border bg-bg-card px-[22px] py-[18px] ${
        isNext ? 'border-accent-editorial' : 'border-border-soft'
      }`}
    >
      {isNext && (
        <span className="mb-3 inline-flex items-center gap-1.5 rounded-full border border-accent-editorial px-3 py-0.5 text-[11px] font-semibold tracking-[0.04em] text-accent-editorial">
          <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent-editorial motion-reduce:animate-none" />
          {nextRaceLabel}
        </span>
      )}
      <h3 className="text-[15px] font-bold tracking-[-0.01em] text-text-primary">{race.raceName}</h3>
      <p className="text-[13px] text-text-secondary">{formatRaceStart(race.raceStart)}</p>
    </article>
  )
}
