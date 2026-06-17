import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { GapList } from './GapList'
import { useLiveRaceStore } from '../store/liveRaceStore'
import type { DriverState } from '../../../shared/types/f1'

function makeDriver(
  driverNumber: number,
  position: number,
  overrides: Partial<DriverState> = {}
): DriverState {
  return {
    driverNumber,
    driverCode: `D${driverNumber}`,
    teamName: 'Team A',
    teamColour: 'FF0000',
    position,
    gapToCarAhead: null,
    gapIsStale: false,
    tyreCompound: null,
    stintLaps: null,
    championshipDelta: null,
    ...overrides,
  }
}

beforeEach(() => {
  useLiveRaceStore.setState({
    drivers: {},
    connectionStatus: 'disconnected',
    lastSnapshotTime: null,
  })
})

describe('GapList', () => {
  it('renders waiting message when no drivers', () => {
    render(<GapList />)
    expect(screen.getByText('Waiting for race data…')).toBeInTheDocument()
  })

  it('renders drivers sorted by position', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 2, { driverCode: 'VER' }),
        '44': makeDriver(44, 1, { driverCode: 'HAM' }),
      },
    })

    render(<GapList />)

    const rows = screen.getAllByText(/HAM|VER/)
    expect(rows[0].textContent).toBe('HAM')
    expect(rows[1].textContent).toBe('VER')
  })

  it('highlights gap under one second as battle', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 2, { gapToCarAhead: '0.456', gapIsStale: false }),
      },
    })

    render(<GapList />)

    expect(screen.getByTestId('battle-gap')).toBeInTheDocument()
    expect(screen.getByTestId('battle-gap').textContent).toBe('0.456')
  })

  it('shows tilde prefix for stale gap', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 2, { gapIsStale: true, gapToCarAhead: null }),
      },
    })

    render(<GapList />)

    expect(screen.getByTestId('stale-gap')).toBeInTheDocument()
    expect(screen.getByTestId('stale-gap').textContent).toBe('~–')
  })

  it('shows Live status when connected', () => {
    useLiveRaceStore.setState({ connectionStatus: 'connected' })

    render(<GapList />)

    expect(screen.getByText('● Live')).toBeInTheDocument()
  })

  it('shows Disconnected status when disconnected', () => {
    useLiveRaceStore.setState({ connectionStatus: 'disconnected' })

    render(<GapList />)

    expect(screen.getByText('○ Disconnected')).toBeInTheDocument()
  })

  it('shows Reconnecting status when reconnecting', () => {
    useLiveRaceStore.setState({ connectionStatus: 'reconnecting' })

    render(<GapList />)

    expect(screen.getByText('◌ Reconnecting…')).toBeInTheDocument()
  })
})
