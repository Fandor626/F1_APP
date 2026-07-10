import { useState } from 'react'
import { useAllDrivers, useHeadToHeadComparison } from '../../shared/api/ergast'
import type { HeadToHeadDriverStats } from '../../shared/api/ergast'
import { DriverSearchSelect } from './DriverSearchSelect'

function formatAverage(value: number | null): string {
  return value == null ? '—' : value.toFixed(1)
}

interface StatRow {
  label: string
  a: string
  b: string
}

function buildStatRows(a: HeadToHeadDriverStats, b: HeadToHeadDriverStats): StatRow[] {
  return [
    { label: 'Qualifying Avg Position', a: formatAverage(a.qualifyingAveragePosition), b: formatAverage(b.qualifyingAveragePosition) },
    { label: 'Race Finish Avg', a: formatAverage(a.raceFinishAveragePosition), b: formatAverage(b.raceFinishAveragePosition) },
    { label: 'DNFs', a: String(a.dnfCount), b: String(b.dnfCount) },
    { label: 'Points Scored', a: String(a.pointsScored), b: String(b.pointsScored) },
    { label: 'Fastest Laps', a: String(a.fastestLaps), b: String(b.fastestLaps) },
    { label: 'Wins', a: String(a.wins), b: String(b.wins) },
  ]
}

export function HeadToHeadPage() {
  const { data: drivers } = useAllDrivers()
  const [driverAId, setDriverAId] = useState<string | null>(null)
  const [driverBId, setDriverBId] = useState<string | null>(null)
  const [season, setSeason] = useState<number | null>(null)
  const [circuitId, setCircuitId] = useState<string | null>(null)

  const { data: comparison, isPending, isError } = useHeadToHeadComparison(driverAId, driverBId, season, circuitId)

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">Head-to-Head</h1>
      <p className="mb-7 text-[13px] text-text-secondary">
        Compare two drivers' stats, optionally filtered by season and/or circuit.
      </p>

      <div className="mb-6 grid grid-cols-2 gap-4">
        <DriverSearchSelect label="Driver A" drivers={drivers ?? []} selectedDriverId={driverAId} onSelect={setDriverAId} />
        <DriverSearchSelect label="Driver B" drivers={drivers ?? []} selectedDriverId={driverBId} onSelect={setDriverBId} />
      </div>

      <div className="mb-7 grid grid-cols-2 gap-4">
        <div>
          <label htmlFor="season-filter" className="mb-1 block text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Season (optional)
          </label>
          <input
            id="season-filter"
            type="number"
            value={season ?? ''}
            onChange={(e) => setSeason(e.target.value ? Number(e.target.value) : null)}
            placeholder="e.g. 2023"
            className="w-full rounded-md border border-border-soft bg-bg-card px-3 py-2 text-[13px] text-text-primary placeholder:text-text-tertiary focus:border-accent-editorial focus:outline-none"
          />
        </div>
        <div>
          <label htmlFor="circuit-filter" className="mb-1 block text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Circuit ID (optional)
          </label>
          <input
            id="circuit-filter"
            type="text"
            value={circuitId ?? ''}
            onChange={(e) => setCircuitId(e.target.value || null)}
            placeholder="e.g. monza"
            className="w-full rounded-md border border-border-soft bg-bg-card px-3 py-2 text-[13px] text-text-primary placeholder:text-text-tertiary focus:border-accent-editorial focus:outline-none"
          />
        </div>
      </div>

      {!driverAId || !driverBId ? (
        <p className="text-[13px] text-text-secondary">Select two drivers to see their stats side by side.</p>
      ) : (
        <>
          {isPending && <p className="text-[13px] text-text-secondary">Loading comparison…</p>}
          {isError && (
            <p role="alert" className="text-[13px] text-text-secondary">
              Couldn't reach the server — try refreshing.
            </p>
          )}
          {comparison === null && (
            <p className="text-[13px] text-text-secondary">No comparison data available for this selection.</p>
          )}
          {comparison && (
            <table className="w-full border-collapse text-[13px]">
              <thead>
                <tr>
                  <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                    Stat
                  </th>
                  <th className="border-b border-border-soft px-2.5 py-2 text-right text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                    {comparison.driverA.fullName}
                  </th>
                  <th className="border-b border-border-soft px-2.5 py-2 text-right text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                    {comparison.driverB.fullName}
                  </th>
                </tr>
              </thead>
              <tbody>
                {buildStatRows(comparison.driverA, comparison.driverB).map((row) => (
                  <tr key={row.label} className="hover:bg-bg-card-hover">
                    <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-text-tertiary">{row.label}</td>
                    <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-right tabular-nums text-text-primary">
                      {row.a}
                    </td>
                    <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-right tabular-nums text-text-primary">
                      {row.b}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </div>
  )
}
