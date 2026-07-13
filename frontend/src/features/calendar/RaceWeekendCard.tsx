import { Link } from 'react-router-dom'
import type { RaceWeekend } from '../../shared/api/ergast'
import { CountryFlag } from '../../shared/components/CountryFlag'
import { describeDaysUntil, formatDateRange, formatRaceTime } from '../../shared/utils/dateUtils'
import { TrackOutline } from './TrackOutline'

interface RaceWeekendCardProps {
  race: RaceWeekend
  isNext?: boolean
}

type LapRecord = NonNullable<RaceWeekend['allTimeLapRecord']>

function FastestLapLine({ label, record }: { label: string; record: LapRecord | null | undefined }) {
  if (!record) return null
  return (
    <div className="flex items-center justify-between py-0.5 text-[12.5px]">
      <span className="text-text-tertiary">{label}</span>
      <span className="tabular-nums text-text-primary">
        {record.time} <span className="text-text-secondary">{record.driverName}</span>
      </span>
    </div>
  )
}

export function RaceWeekendCard({ race, isNext = false }: RaceWeekendCardProps) {
  const nextRaceLabel = `Next race: ${race.raceName}, ${describeDaysUntil(new Date(race.raceStart), new Date())}`
  const hasFastestLapData = race.allTimeLapRecord || race.recentLapRecord

  return (
    <Link
      to={`/races/${race.round}`}
      data-testid="race-weekend-card"
      className={`flex gap-4 rounded-lg border bg-bg-card px-[22px] py-[18px] no-underline hover:border-accent-editorial ${
        isNext ? 'border-accent-editorial' : 'border-border-soft'
      }`}
    >
      <div className="flex w-[168px] shrink-0 items-center justify-center rounded-md bg-bg-inset p-3 text-text-tertiary">
        <TrackOutline circuitId={race.circuitId} circuitName={race.circuitName} className="h-full w-full" />
      </div>

      <div className="min-w-0 flex-1">
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

        {hasFastestLapData && (
          <div className="mt-3 border-t border-border-soft pt-2">
            <FastestLapLine label="All-time fastest lap" record={race.allTimeLapRecord} />
            <FastestLapLine
              label={race.recentLapRecord ? `${race.recentLapRecord.season} fastest lap` : ''}
              record={race.recentLapRecord}
            />
          </div>
        )}
      </div>
    </Link>
  )
}
