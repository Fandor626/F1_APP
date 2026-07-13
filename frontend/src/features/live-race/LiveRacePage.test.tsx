import { render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { LiveRacePage } from './LiveRacePage'
import { useLiveRaceStore } from './store/liveRaceStore'

vi.mock('./hooks/useSignalRConnection', () => ({
  useSignalRConnection: vi.fn(),
}))

vi.mock('./hooks/useLastRaceResult', () => ({
  useLastRaceResult: vi.fn().mockReturnValue({ data: undefined, isPending: false, isFetched: false }),
}))

vi.mock('../../shared/api/ergast', () => ({
  useRaceSchedule: vi.fn().mockReturnValue({ data: undefined }),
}))

function renderWithQueryClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  )
}

beforeEach(() => {
  useLiveRaceStore.setState({
    connectionStatus: 'disconnected',
    sessionMode: 'live',
    fallbackRaceName: null,
    circuitId: null,
    drivers: {},
    lapChart: {},
    lastSnapshotTime: null,
  })
  vi.clearAllMocks()
})

describe('LiveRacePage', () => {
  it('shows no banner in live mode', () => {
    useLiveRaceStore.setState({ sessionMode: 'live' })
    renderWithQueryClient(<LiveRacePage />)
    expect(screen.queryByTestId('fallback-banner')).not.toBeInTheDocument()
    expect(screen.queryByTestId('stale-banner')).not.toBeInTheDocument()
  })

  it('shows past race banner with race name in fallback mode', () => {
    useLiveRaceStore.setState({ sessionMode: 'fallback', fallbackRaceName: 'Canadian Grand Prix' })
    renderWithQueryClient(<LiveRacePage />)
    expect(screen.getByTestId('fallback-banner')).toBeInTheDocument()
    expect(screen.getByText(/Canadian Grand Prix/)).toBeInTheDocument()
  })

  it('shows generic past race label when fallback race name is not yet known', () => {
    useLiveRaceStore.setState({ sessionMode: 'fallback', fallbackRaceName: null })
    renderWithQueryClient(<LiveRacePage />)
    expect(screen.getByTestId('fallback-banner')).toBeInTheDocument()
    expect(screen.getByText(/Last Race/)).toBeInTheDocument()
  })

  it('shows stale data warning in stale mode', () => {
    useLiveRaceStore.setState({ sessionMode: 'stale' })
    renderWithQueryClient(<LiveRacePage />)
    expect(screen.getByTestId('stale-banner')).toBeInTheDocument()
    expect(screen.getByText(/data may be delayed/i)).toBeInTheDocument()
  })

  it('shows no fallback banner in stale mode (only stale banner)', () => {
    useLiveRaceStore.setState({ sessionMode: 'stale' })
    renderWithQueryClient(<LiveRacePage />)
    expect(screen.queryByTestId('fallback-banner')).not.toBeInTheDocument()
    expect(screen.getByTestId('stale-banner')).toBeInTheDocument()
  })

  it('populates store with REST fallback data when in fallback mode and no drivers', async () => {
    const { useLastRaceResult } = await import('./hooks/useLastRaceResult')
    vi.mocked(useLastRaceResult).mockReturnValue({
      data: {
        raceName: 'Austrian Grand Prix',
        raceDate: '2026-07-06',
        drivers: [
          {
            driverNumber: 4, driverCode: 'NOR', teamName: 'McLaren',
            teamColour: '555555', position: 1, gapToCarAhead: null,
            gapIsStale: false, tyreCompound: null, stintLaps: null, championshipDelta: null,
            x: null, y: null, miniSectorStatus: null, pitWindowActive: false,
          },
        ],
      } as never,
      isPending: false,
    } as never)

    useLiveRaceStore.setState({ sessionMode: 'fallback', drivers: {} })
    renderWithQueryClient(<LiveRacePage />)

    await waitFor(() => {
      const { drivers } = useLiveRaceStore.getState()
      expect(Object.keys(drivers)).toHaveLength(1)
      expect(drivers['4']?.driverCode).toBe('NOR')
    })
  })

  it('shows a plain on-brand message naming the next race when zero races have completed this season', async () => {
    const { useLastRaceResult } = await import('./hooks/useLastRaceResult')
    vi.mocked(useLastRaceResult).mockReturnValue({ data: null, isPending: false, isFetched: true } as never)

    const { useRaceSchedule } = await import('../../shared/api/ergast')
    vi.mocked(useRaceSchedule).mockReturnValue({
      data: [
        {
          season: 2026, round: 1, raceName: 'Bahrain Grand Prix', circuitId: 'bahrain',
          circuitName: 'Bahrain International Circuit', locality: 'Sakhir', country: 'Bahrain',
          weekendStart: '2099-03-06T13:30:00+00:00', raceStart: '2099-03-08T18:00:00+00:00',
        },
      ],
    } as never)

    useLiveRaceStore.setState({ sessionMode: 'fallback', drivers: {} })
    renderWithQueryClient(<LiveRacePage />)

    const message = screen.getByTestId('no-races-yet')
    expect(message).toHaveTextContent('No races completed yet this season')
    expect(message).toHaveTextContent('Bahrain Grand Prix')
  })

  it('does not show the zero-races message once real fallback data has arrived', async () => {
    const { useLastRaceResult } = await import('./hooks/useLastRaceResult')
    vi.mocked(useLastRaceResult).mockReturnValue({ data: null, isPending: false, isFetched: true } as never)

    useLiveRaceStore.setState({
      sessionMode: 'fallback',
      drivers: {
        '4': {
          driverNumber: 4, driverCode: 'NOR', teamName: 'McLaren', teamColour: '555555',
          position: 1, gapToCarAhead: null, gapIsStale: false, tyreCompound: null,
          stintLaps: null, championshipDelta: null, x: null, y: null,
          miniSectorStatus: null, pitWindowActive: false,
        },
      },
    })
    renderWithQueryClient(<LiveRacePage />)

    expect(screen.queryByTestId('no-races-yet')).not.toBeInTheDocument()
  })
})
