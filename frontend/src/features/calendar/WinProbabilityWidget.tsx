import type { WinProbabilityEntry } from '../../shared/api/ergast'

interface WinProbabilityWidgetProps {
  entries: WinProbabilityEntry[]
}

export function WinProbabilityWidget({ entries }: WinProbabilityWidgetProps) {
  if (entries.length === 0) return null

  return (
    <div className="mt-8">
      <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
        Win Probability
      </h2>
      <ul className="flex flex-col gap-2">
        {entries.map((entry) => (
          <li key={`${entry.gridPosition}-${entry.driverName}`} className="flex items-baseline gap-2">
            <span className="w-6 text-[13px] text-text-secondary">P{entry.gridPosition}</span>
            <span className="text-[13px] font-semibold text-text-primary">{entry.driverName}</span>
            <span className="text-[13px] text-text-secondary">({entry.constructorName})</span>
            <span className="ml-auto text-[13px] tabular-nums text-text-secondary">
              {entry.winProbability.toFixed(1)}%
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}
