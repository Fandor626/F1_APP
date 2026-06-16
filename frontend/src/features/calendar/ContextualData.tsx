import type { ChampionshipDelta, PriorYearWinner } from '../../shared/api/ergast'

interface ContextualDataProps {
  priorYearWinner?: PriorYearWinner
  championshipDelta?: ChampionshipDelta
}

export function ContextualData({ priorYearWinner, championshipDelta }: ContextualDataProps) {
  return (
    <div className="mt-8 flex flex-col gap-6">
      <div>
        <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
          Last Year's Winner
        </h2>
        {priorYearWinner ? (
          <p className="text-[13px] text-text-primary">
            <span className="font-semibold">{priorYearWinner.driverName}</span>{' '}
            <span className="text-text-secondary">({priorYearWinner.constructorName})</span>
            {priorYearWinner.time && <span className="text-text-secondary"> · {priorYearWinner.time}</span>}
          </p>
        ) : (
          <p className="text-[13px] text-text-secondary">First race at this circuit.</p>
        )}
      </div>

      {championshipDelta && (
        <div>
          <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Championship Gap
          </h2>
          <p className="text-[13px] text-text-primary">
            <span className="font-semibold">{championshipDelta.leaderName}</span> leads{' '}
            <span className="font-semibold">{championshipDelta.runnerUpName}</span> by{' '}
            <span className="tabular-nums">{championshipDelta.pointsGap}</span> pts
          </p>
        </div>
      )}
    </div>
  )
}
