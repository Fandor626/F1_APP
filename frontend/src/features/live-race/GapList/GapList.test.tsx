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
    x: null,
    y: null,
    miniSectorStatus: null,
    pitWindowActive: false,
    ...overrides,
  }
}

beforeEach(() => {
  useLiveRaceStore.setState({
    drivers: {},
    connectionStatus: 'disconnected',
    lastSnapshotTime: null,
    sessionMode: 'live',
    fallbackRaceName: null,
    circuitId: null,
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

  it('shows tyre compound circle when compound data is present', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: 'SOFT' }),
      },
    })
    render(<GapList />)
    const tyreCircle = screen.getByTestId('tyre-compound')
    expect(tyreCircle).toBeInTheDocument()
    // SOFT = #E8002D red
    expect(tyreCircle).toHaveStyle({ backgroundColor: 'rgb(232, 0, 45)' })
  })

  it('shows stint laps when both compound and lap data are present', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: 'MEDIUM', stintLaps: 12 }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('stint-laps').textContent).toBe('12')
  })

  it('hides tyre section when compound is null', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: null, stintLaps: null }),
      },
    })
    render(<GapList />)
    expect(screen.queryByTestId('tyre-compound')).not.toBeInTheDocument()
    expect(screen.queryByTestId('stint-laps')).not.toBeInTheDocument()
  })

  it('hides stint laps when lap count is null even if compound is set', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { tyreCompound: 'HARD', stintLaps: null }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('tyre-compound')).toBeInTheDocument()
    expect(screen.queryByTestId('stint-laps')).not.toBeInTheDocument()
  })

  it('shows championship delta when set', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { championshipDelta: '+45' }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('championship-delta').textContent).toBe('+45')
  })

  it('hides championship delta when null', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { championshipDelta: null }),
      },
    })
    render(<GapList />)
    expect(screen.queryByTestId('championship-delta')).not.toBeInTheDocument()
  })

  it('shows negative delta for trailing driver', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { championshipDelta: '−12' }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('championship-delta').textContent).toBe('−12')
  })

  it('renders "pts if race ended now" label in header', () => {
    render(<GapList />)
    expect(screen.getByText('pts if race ended now')).toBeInTheDocument()
  })

  it('shows pit window indicator when active', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { pitWindowActive: true }),
      },
    })
    render(<GapList />)
    expect(screen.getByTestId('pit-window-indicator')).toBeInTheDocument()
  })

  it('hides pit window indicator when inactive', () => {
    useLiveRaceStore.setState({
      drivers: {
        '33': makeDriver(33, 1, { pitWindowActive: false }),
      },
    })
    render(<GapList />)
    expect(screen.queryByTestId('pit-window-indicator')).not.toBeInTheDocument()
  })
})
