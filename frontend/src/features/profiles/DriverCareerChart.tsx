import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { DriverCareerPoint } from '../../shared/api/ergast'

// Same visual style as standings/TrajectoryChart.tsx (Story 4.2), per AC 2 —
// same card wrapper, axis/grid tokens, and tooltip layout, adapted to a
// single line since a career spans many seasons (round numbers alone
// aren't unique/monotonic across seasons, so the X axis is a sequential
// career-race index instead of "round").
interface ChartPoint {
  index: number
  cumulativePoints: number
}

function buildChartData(points: DriverCareerPoint[]): ChartPoint[] {
  return points.map((p, i) => ({ index: i + 1, cumulativePoints: p.cumulativePoints }))
}

interface TooltipPayloadEntry {
  value: number
}

export interface DriverCareerTooltipProps {
  active?: boolean
  payload?: TooltipPayloadEntry[]
  label?: number
  points: DriverCareerPoint[]
}

export function DriverCareerTooltip({ active, payload, label, points }: DriverCareerTooltipProps) {
  if (!active || !payload?.length || label == null) return null
  const point = points[label - 1]
  if (!point) return null

  return (
    <div className="rounded-lg border border-border-soft bg-bg-card px-3 py-2 text-[11px]">
      <p className="mb-1 font-semibold text-text-secondary">
        {point.season} · {point.raceName}
      </p>
      <div className="flex items-center gap-2">
        <span className="tabular-nums text-text-secondary">+{point.pointsThisRound}</span>
        <span className="tabular-nums text-text-primary">{point.cumulativePoints} pts</span>
      </div>
    </div>
  )
}

function DriverCareerChartSkeleton() {
  return (
    <div aria-hidden="true" className="h-[260px] animate-pulse rounded-md bg-bg-inset motion-reduce:animate-none" />
  )
}

interface DriverCareerChartProps {
  points: DriverCareerPoint[]
  isPending: boolean
  isError: boolean
}

export function DriverCareerChart({ points, isPending, isError }: DriverCareerChartProps) {
  const chartData = buildChartData(points)

  return (
    <div className="mt-6 rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
      <div className="mb-1 text-[15px] font-bold tracking-[-0.01em] text-text-primary">Career Points Progression</div>
      <p className="mb-3.5 text-[12px] text-text-tertiary">Cumulative points across every race entered</p>

      {isPending && <DriverCareerChartSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}
      {!isPending && !isError && points.length === 0 && (
        <p className="text-[13px] text-text-secondary">No race results on record.</p>
      )}
      {!isPending && !isError && points.length > 0 && (
        <div className="rounded-md border border-border-soft bg-bg-inset p-2">
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={chartData} margin={{ top: 4, right: 8, bottom: 4, left: 8 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--color-bg-card-hover)" />
              <XAxis
                dataKey="index"
                tick={{ fill: 'var(--color-text-tertiary)', fontSize: 10 }}
                label={{ value: 'Career race #', position: 'insideBottomRight', offset: -4, fill: 'var(--color-text-tertiary)', fontSize: 10 }}
              />
              <YAxis
                tick={{ fill: 'var(--color-text-tertiary)', fontSize: 10 }}
                width={40}
                domain={['auto', 'auto']}
              />
              <Tooltip content={<DriverCareerTooltip points={points} />} />
              <Line
                type="monotone"
                dataKey="cumulativePoints"
                stroke="var(--color-accent-editorial)"
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 3 }}
                connectNulls={false}
                isAnimationActive={false}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
