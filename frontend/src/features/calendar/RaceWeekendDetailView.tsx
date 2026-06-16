import { Link, useParams } from 'react-router-dom'
import { useRaceDetail } from '../../shared/api/ergast'
import { CountryFlag } from '../../shared/components/CountryFlag'
import { formatSessionTime } from '../../shared/utils/dateUtils'
import { ContextualData } from './ContextualData'

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
          <p className="mb-7 text-[13px] text-text-secondary">{data.circuitName}</p>

          <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Sessions
          </h2>
          <ul className="flex flex-col gap-2">
            {data.sessions.map((session) => (
              <li
                key={session.name}
                className="flex items-center justify-between rounded-lg border border-border-soft bg-bg-card px-[22px] py-3"
              >
                <span className="text-[13px] font-semibold text-text-primary">{session.name}</span>
                <span className="text-[13px] text-text-secondary">{formatSessionTime(session.start)}</span>
              </li>
            ))}
          </ul>

          <ContextualData priorYearWinner={data.priorYearWinner} championshipDelta={data.championshipDelta} />
        </>
      )}
    </div>
  )
}
