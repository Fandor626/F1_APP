import { render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { LiveRacePage } from './LiveRacePage'
import { useLiveRaceStore } from './store/liveRaceStore'

vi.mock('./hooks/useSignalRConnection', () => ({
  useSignalRConnection: vi.fn(),
}))

vi.mock('./hooks/useLastRaceResult', () => ({
  useLastRaceResult: vi.fn().mockReturnValue({ data: null, isPending: false }),
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
            x: null, y: null, miniSectorStatus: null,
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
})
