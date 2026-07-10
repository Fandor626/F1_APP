import { useMemo } from 'react'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useChampionshipTrajectory } from '../../shared/api/ergast'
import type { DriverTrajectory } from '../../shared/api/ergast'
import { constructorColor } from './constructorColors'

interface ChartPoint {
  round: number
  [driverId: string]: number
}

function buildChartData(trajectories: DriverTrajectory[]): ChartPoint[] {
  const allRounds = [...new Set(trajectories.flatMap((t) => t.points.map((p) => p.round)))].sort((a, b) => a - b)

  return allRounds.map((round) => {
    const point: ChartPoint = { round }
    for (const trajectory of trajectories) {
      const atRound = trajectory.points.find((p) => p.round === round)
      if (atRound) point[trajectory.driverId] = atRound.cumulativePoints
    }
    return point
  })
}

interface TooltipPayloadEntry {
  dataKey: string
  value: number
  color: string
}

export interface TrajectoryTooltipProps {
  active?: boolean
  payload?: TooltipPayloadEntry[]
  label?: number
  trajectories: DriverTrajectory[]
}

export function TrajectoryTooltip({ active, payload, label, trajectories }: TrajectoryTooltipProps) {
  if (!active || !payload?.length || label == null) return null

  return (
    <div className="rounded-lg border border-border-soft bg-bg-card px-3 py-2 text-[11px]">
      <p className="mb-1 font-semibold text-text-secondary">Round {label}</p>
      {payload.map((entry) => {
        const trajectory = trajectories.find((t) => t.driverId === entry.dataKey)
        const point = trajectory?.points.find((p) => p.round === label)
        if (!trajectory || !point) return null

        return (
          <div key={entry.dataKey} className="mb-0.5 flex items-center gap-2">
            <span
              aria-hidden="true"
              className="inline-block h-[7px] w-[7px] shrink-0 rounded-full"
              style={{ backgroundColor: entry.color }}
            />
            <span className="font-semibold text-text-primary">{trajectory.driverName}</span>
            <span className="text-text-secondary">{point.raceName}</span>
            {point.resultPosition != null && <span className="text-text-secondary">P{point.resultPosition}</span>}
            <span className="tabular-nums text-text-secondary">+{point.pointsThisRound}</span>
            <span className="tabular-nums text-text-primary">{point.cumulativePoints} pts</span>
          </div>
        )
      })}
    </div>
  )
}

function TrajectoryChartSkeleton() {
  return (
    <div aria-hidden="true" className="h-[260px] animate-pulse rounded-md bg-bg-inset motion-reduce:animate-none" />
  )
}

export function TrajectoryChart() {
  const { data, isPending, isError } = useChampionshipTrajectory()

  const chartData = useMemo(() => buildChartData(data ?? []), [data])

  return (
    <div className="mt-6 rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
      <div className="mb-1 text-[15px] font-bold tracking-[-0.01em] text-text-primary">Championship Trajectory</div>
      <p className="mb-3.5 text-[12px] text-text-tertiary">Cumulative points by round</p>

      {isPending && <TrajectoryChartSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}
      {data && data.length === 0 && (
        <p className="text-[13px] text-text-secondary">No completed rounds yet.</p>
      )}
      {data && data.length > 0 && (
        <div className="rounded-md border border-border-soft bg-bg-inset p-2">
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={chartData} margin={{ top: 4, right: 8, bottom: 4, left: 8 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--color-bg-card-hover)" />
              <XAxis
                dataKey="round"
                tick={{ fill: 'var(--color-text-tertiary)', fontSize: 10 }}
                label={{ value: 'Round', position: 'insideBottomRight', offset: -4, fill: 'var(--color-text-tertiary)', fontSize: 10 }}
              />
              <YAxis
                tick={{ fill: 'var(--color-text-tertiary)', fontSize: 10 }}
                width={40}
                domain={['auto', 'auto']}
              />
              <Tooltip content={<TrajectoryTooltip trajectories={data} />} />
              {data.map((trajectory) => (
                <Line
                  key={trajectory.driverId}
                  type="monotone"
                  dataKey={trajectory.driverId}
                  name={trajectory.driverName}
                  stroke={constructorColor(trajectory.constructorName)}
                  strokeWidth={2}
                  dot={false}
                  activeDot={{ r: 3 }}
                  connectNulls={false}
                  isAnimationActive={false}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
