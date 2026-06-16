import { Link } from 'react-router-dom'
import { useConstructorStandings, useDriverStandings, type RaceWeekend } from '../../shared/api/ergast'
import { CountryFlag } from '../../shared/components/CountryFlag'
import { describeDaysUntil, formatDateRange, formatRaceTime } from '../../shared/utils/dateUtils'

interface RaceWeekendCardProps {
  race: RaceWeekend
  isNext?: boolean
}

interface StandingRow {
  position: number
  name: string
  points: number
}

function StandingsColumn({ title, rows }: { title: string; rows: StandingRow[] }) {
  return (
    <div>
      <div className="mb-2 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
        {title}
      </div>
      {rows.map((row) => (
        <div key={row.position} className="flex items-center justify-between py-1 text-[13px]">
          <span className="flex items-center gap-2">
            <span className="w-3.5 text-[12px] text-text-tertiary">{row.position}</span>
            <span className="text-text-primary">{row.name}</span>
          </span>
          <span className="text-[12.5px] text-text-secondary tabular-nums">{row.points} pts</span>
        </div>
      ))}
    </div>
  )
}

export function RaceWeekendCard({ race, isNext = false }: RaceWeekendCardProps) {
  const { data: driverStandings } = useDriverStandings()
  const { data: constructorStandings } = useConstructorStandings()

  const nextRaceLabel = `Next race: ${race.raceName}, ${describeDaysUntil(new Date(race.raceStart), new Date())}`

  return (
    <Link
      to={`/races/${race.round}`}
      data-testid="race-weekend-card"
      className={`block rounded-lg border bg-bg-card px-[22px] py-[18px] no-underline hover:border-accent-editorial ${
        isNext ? 'border-accent-editorial' : 'border-border-soft'
      }`}
    >
      {isNext && (
        <span className="mb-3 inline-flex items-center gap-1.5 rounded-full border border-accent-editorial px-3 py-0.5 text-[11px] font-semibold tracking-[0.04em] text-accent-editorial">
          <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent-editorial motion-reduce:animate-none" />
          {nextRaceLabel}
        </span>
      )}

      <div className="mb-1 flex items-baseline gap-2">
        <CountryFlag country={race.country} className="text-[18px] leading-none" />
        <h3 className="text-[15px] font-bold tracking-[-0.01em] text-text-primary">{race.raceName}</h3>
      </div>
      <p className="text-[12px] text-text-tertiary">{race.circuitName}</p>
      <p className="text-[13px] text-text-secondary">
        {formatDateRange(new Date(race.weekendStart), new Date(race.raceStart))} · Race:{' '}
        {formatRaceTime(race.raceStart)}
      </p>

      {driverStandings && constructorStandings && (
        <div className="mt-3 grid grid-cols-2 gap-4 border-t border-border-soft pt-3">
          <StandingsColumn
            title="Drivers' Championship"
            rows={driverStandings
              .slice(0, 3)
              .map((standing) => ({ position: standing.position, name: standing.driverName, points: standing.points }))}
          />
          <StandingsColumn
            title="Constructors' Championship"
            rows={constructorStandings
              .slice(0, 3)
              .map((standing) => ({ position: standing.position, name: standing.constructorName, points: standing.points }))}
          />
        </div>
      )}
    </Link>
  )
}
