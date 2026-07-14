import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useRaceDetail, useWinProbability } from '../../shared/api/ergast'
import { CountryFlag } from '../../shared/components/CountryFlag'
import { formatSessionTimeForMode } from '../../shared/utils/dateUtils'
import { ContextualData } from './ContextualData'
import { TimezoneToggle } from './TimezoneToggle'
import { TrackOutline } from './TrackOutline'
import { TrackRecords } from './TrackRecords'
import { WinProbabilityWidget } from './WinProbabilityWidget'

function SessionsSkeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-2">
      {[0, 1, 2, 3, 4].map((key) => (
        <div
          key={key}
          className="h-12 animate-pulse rounded-lg border border-border-soft bg-bg-card motion-reduce:animate-none"
        />
      ))}
    </div>
  )
}

export function RaceWeekendDetailView() {
  const { round } = useParams<{ round: string }>()
  const { data, isPending, isError } = useRaceDetail(Number(round))
  const { data: winProbs } = useWinProbability(Number(round))
  const [tzMode, setTzMode] = useState<'local' | 'track'>('local')

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <Link
        to="/"
        className="mb-6 inline-block text-[13px] text-text-secondary hover:text-accent-editorial"
      >
        ← Calendar
      </Link>

      {isPending && <SessionsSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}

      {data && (
        <>
          <div className="mb-1 flex items-baseline gap-2">
            <CountryFlag country={data.country} className="text-[20px] leading-none" />
            <h1 className="text-[26px] font-bold tracking-[-0.01em] text-text-primary">{data.raceName}</h1>
          </div>
          <Link
            to={`/circuits/${data.circuitId}`}
            className="mb-7 inline-block text-[13px] text-text-secondary hover:text-accent-editorial hover:underline"
          >
            {data.circuitName}
          </Link>

          <div
            className="mb-7 flex items-center justify-center rounded-lg border border-border-soft bg-bg-inset p-4"
            data-testid="race-weekend-track-layout"
          >
            <TrackOutline
              circuitId={data.circuitId}
              circuitName={data.circuitName}
              className="h-[320px] w-full max-w-[560px] text-text-secondary"
            />
          </div>

          <TrackRecords allTimeLapRecord={data.allTimeLapRecord} recentLapRecord={data.recentLapRecord} />

          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
              Sessions
            </h2>
            <TimezoneToggle mode={tzMode} onToggle={() => setTzMode(m => (m === 'local' ? 'track' : 'local'))} />
          </div>
          <ul className="flex flex-col gap-2">
            {data.sessions.map((session) => (
              <li
                key={session.name}
                className="flex items-center justify-between rounded-lg border border-border-soft bg-bg-card px-[22px] py-3"
              >
                <span className="text-[13px] font-semibold text-text-primary">{session.name}</span>
                <span className="text-[13px] text-text-secondary">{formatSessionTimeForMode(session.start, tzMode)}</span>
              </li>
            ))}
          </ul>

          <ContextualData priorYearWinner={data.priorYearWinner} championshipDelta={data.championshipDelta} />
          {winProbs && winProbs.length > 0 && <WinProbabilityWidget entries={winProbs} />}
        </>
      )}
    </div>
  )
}
