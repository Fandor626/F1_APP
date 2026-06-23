import { useMemo } from 'react'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts'
import { useShallow } from 'zustand/react/shallow'
import { useLiveRaceStore } from '../store/liveRaceStore'
import type { LapTimeEntry } from '../../../shared/types/f1'

function formatLapTime(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}:${s.toFixed(3).padStart(6, '0')}`
}

interface ChartPoint {
  lap: number
  [driverNum: string]: number | undefined
}

function buildChartData(lapChart: Record<string, LapTimeEntry[]>): ChartPoint[] {
  // Pre-build per-driver Maps for O(1) lap lookup instead of O(L) find()
  const driverMaps = Object.entries(lapChart).map(([driverNum, laps]) => ({
    driverNum,
    byLap: new Map(laps.map(l => [l.lapNumber, l])),
  }))

  const allLapNums = [
    ...new Set(
      Object.values(lapChart).flatMap(laps => laps.map(l => l.lapNumber))
    ),
  ].sort((a, b) => a - b)

  return allLapNums.map(lapNum => {
    const point: ChartPoint = { lap: lapNum }
    for (const { driverNum, byLap } of driverMaps) {
      const entry = byLap.get(lapNum)
      if (entry?.lapDurationSeconds != null) {
        point[driverNum] = entry.lapDurationSeconds
      }
    }
    return point
  })
}

function findFastestLap(lapChart: Record<string, LapTimeEntry[]>): number | null {
  const allTimes = Object.values(lapChart)
    .flatMap(laps => laps)
    .filter(l => !l.isPitOutLap && l.lapDurationSeconds != null)
    .map(l => l.lapDurationSeconds!)
  return allTimes.length > 0 ? Math.min(...allTimes) : null
}

interface CustomTooltipProps {
  active?: boolean
  payload?: Array<{ name: string; value: number; color: string }>
  label?: number
  fastestLap: number | null
}

function CustomTooltip({ active, payload, label, fastestLap }: CustomTooltipProps) {
  if (!active || !payload?.length) return null
  return (
    <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[8px] px-3 py-2 text-[11px]">
      <p className="text-[#9aa1ad] mb-1 font-semibold">Lap {label}</p>
      {payload.map(entry => {
        const gap =
          fastestLap != null && entry.value !== fastestLap
            ? `+${(entry.value - fastestLap).toFixed(3)}s`
            : 'fastest'
        return (
          <div key={entry.name} className="flex items-center gap-2 mb-0.5">
            <span
              className="inline-block rounded-full shrink-0"
              style={{ width: 7, height: 7, backgroundColor: entry.color }}
            />
            <span className="text-[#eef0f3] tabular-nums">
              {formatLapTime(entry.value)}
            </span>
            <span className="text-[#9aa1ad] tabular-nums">{gap}</span>
          </div>
        )
      })}
    </div>
  )
}

export function LapTimeChart() {
  const lapChart = useLiveRaceStore(s => s.lapChart)
  const driverColours = useLiveRaceStore(
    useShallow(s => {
      const meta: Record<string, string> = {}
      for (const [id, d] of Object.entries(s.drivers)) {
        meta[id] = d.teamColour
      }
      return meta
    })
  )

  const driverNums = Object.keys(lapChart)

  const chartData = useMemo(() => buildChartData(lapChart), [lapChart])
  const fastestLap = useMemo(() => findFastestLap(lapChart), [lapChart])

  if (driverNums.length === 0) {
    return (
      <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[14px] p-[10px]">
        <span className="text-[11.5px] font-semibold tracking-[0.04em] uppercase text-[#9aa1ad] block mb-3">
          Lap Times
        </span>
        <p className="px-[10px] py-6 text-[12px] text-[#6b7280]">
          Waiting for lap data…
        </p>
      </div>
    )
  }

  return (
    <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[14px] p-[10px]">
      <span className="text-[11.5px] font-semibold tracking-[0.04em] uppercase text-[#9aa1ad] block mb-3">
        Lap Times
      </span>
      <ResponsiveContainer width="100%" height={280}>
        <LineChart data={chartData} margin={{ top: 4, right: 8, bottom: 4, left: 8 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#2a2f38" />
          <XAxis
            dataKey="lap"
            tick={{ fill: '#9aa1ad', fontSize: 10 }}
            label={{ value: 'Lap', position: 'insideBottomRight', offset: -4, fill: '#9aa1ad', fontSize: 10 }}
          />
          <YAxis
            tickFormatter={formatLapTime}
            tick={{ fill: '#9aa1ad', fontSize: 10 }}
            width={52}
            domain={['auto', 'auto']}
          />
          <Tooltip
            content={<CustomTooltip fastestLap={fastestLap} />}
          />
          {driverNums.map(driverNum => (
            <Line
              key={driverNum}
              type="monotone"
              dataKey={driverNum}
              stroke={`#${driverColours[driverNum] ?? '555555'}`}
              strokeWidth={1.5}
              dot={false}
              activeDot={{ r: 3 }}
              connectNulls={false}
              isAnimationActive={false}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
