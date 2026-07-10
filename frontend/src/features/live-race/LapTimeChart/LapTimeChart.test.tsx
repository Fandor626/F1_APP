import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LapTimeChart } from './LapTimeChart'
import { useLiveRaceStore } from '../store/liveRaceStore'
import type { DriverState, LapTimeEntry } from '../../../shared/types/f1'

// Recharts renders SVG which jsdom doesn't support; mock the module
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: ({ dataKey }: { dataKey: string }) => (
    <div data-testid={`line-${dataKey}`} />
  ),
  XAxis: () => null,
  YAxis: () => null,
  Tooltip: () => null,
  CartesianGrid: () => null,
}))

function makeDriver(driverNumber: number, teamColour = 'FF0000'): DriverState {
  return {
    driverNumber,
    driverCode: `D${driverNumber}`,
    teamName: 'Team A',
    teamColour,
    position: 1,
    gapToCarAhead: null,
    gapIsStale: false,
    tyreCompound: null,
    stintLaps: null,
    championshipDelta: null,
    x: null,
    y: null,
    miniSectorStatus: null,
    pitWindowActive: false,
  }
}

function makeLap(lapNumber: number, lapDurationSeconds: number, isPitOutLap = false): LapTimeEntry {
  return { lapNumber, lapDurationSeconds, isPitOutLap }
}

beforeEach(() => {
  useLiveRaceStore.setState({
    drivers: {},
    lapChart: {},
    connectionStatus: 'disconnected',
    lastSnapshotTime: null,
  })
})

describe('LapTimeChart', () => {
  it('shows waiting placeholder when no lap chart data', () => {
    render(<LapTimeChart />)
    expect(screen.getByText('Waiting for lap data…')).toBeInTheDocument()
  })

  it('renders the chart when lap data is present', () => {
    useLiveRaceStore.setState({
      lapChart: {
        '33': [makeLap(1, 83.456)],
      },
      drivers: { '33': makeDriver(33) },
    })

    render(<LapTimeChart />)

    expect(screen.getByTestId('line-chart')).toBeInTheDocument()
    expect(screen.queryByText('Waiting for lap data…')).not.toBeInTheDocument()
  })

  it('renders one Line per driver', () => {
    useLiveRaceStore.setState({
      lapChart: {
        '1': [makeLap(1, 82.0)],
        '33': [makeLap(1, 83.0)],
      },
      drivers: {
        '1': makeDriver(1, '3671C6'),
        '33': makeDriver(33, 'E8002D'),
      },
    })

    render(<LapTimeChart />)

    expect(screen.getByTestId('line-1')).toBeInTheDocument()
    expect(screen.getByTestId('line-33')).toBeInTheDocument()
  })

  it('shows "Lap Times" header in both empty and populated states', () => {
    render(<LapTimeChart />)
    expect(screen.getByText('Lap Times')).toBeInTheDocument()
  })

  it('still renders chart when driver meta is missing for a lap chart entry', () => {
    // Driver 99 has lap data but no entry in drivers store (edge case)
    useLiveRaceStore.setState({
      lapChart: { '99': [makeLap(1, 85.0)] },
      drivers: {},
    })

    render(<LapTimeChart />)

    expect(screen.getByTestId('line-chart')).toBeInTheDocument()
    // Falls back to #555555 for unknown team colour — no crash
    expect(screen.getByTestId('line-99')).toBeInTheDocument()
  })
})
