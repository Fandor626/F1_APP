import { Link } from 'react-router-dom'
import type { CircuitStats, CircuitWinner } from '../../shared/api/ergast'

interface CircuitHistoryProps {
  firstF1Season?: number | null
  stats?: CircuitStats | null
  pastWinners?: CircuitWinner[]
}

function StatTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-border-soft bg-bg-inset px-4 py-3 text-center">
      <div className="text-[18px] font-bold tabular-nums text-text-primary">{value}</div>
      <div className="mt-1 text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">{label}</div>
    </div>
  )
}

// Deliberately a separate panel from TrackRecords, per UX-DR9's four bg-inset
// stat tiles above a past-winners list.
export function CircuitHistory({ firstF1Season, stats, pastWinners }: CircuitHistoryProps) {
  const hasStats = !!stats || !!firstF1Season
  const hasWinners = !!pastWinners && pastWinners.length > 0

  if (!hasStats && !hasWinners) return null

  return (
    <div className="mb-7">
      <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
        Circuit History
      </h2>

      {hasStats && (
        <div className="mb-4 grid grid-cols-4 gap-2">
          <StatTile label="Length" value={stats ? `${stats.lengthKm} km` : '—'} />
          <StatTile label="Corners" value={stats ? String(stats.corners) : '—'} />
          <StatTile label="DRS Zones" value={stats ? String(stats.drsZones) : '—'} />
          <StatTile label="First F1 Race" value={firstF1Season ? String(firstF1Season) : '—'} />
        </div>
      )}

      {hasWinners && (
        <table className="w-full border-collapse text-[13px]">
          <thead>
            <tr>
              <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                Year
              </th>
              <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                Driver
              </th>
              <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                Team
              </th>
            </tr>
          </thead>
          <tbody>
            {pastWinners.map((winner) => (
              <tr key={winner.season} className="hover:bg-bg-card-hover">
                <td className="border-b border-bg-card-hover px-2.5 py-[11px] font-semibold text-text-tertiary">
                  {winner.season}
                </td>
                <td className="border-b border-bg-card-hover px-2.5 py-[11px] font-semibold text-text-primary">
                  <Link to={`/drivers/${winner.driverId}`} className="hover:text-accent-editorial hover:underline">
                    {winner.driverName}
                  </Link>
                </td>
                <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-text-secondary">
                  {winner.constructorName}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
