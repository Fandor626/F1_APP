import { render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TrackMap } from './TrackMap'
import { useLiveRaceStore } from '../store/liveRaceStore'

const mockConfig = {
  circuitId: 'monza',
  viewBox: '0 0 900 600',
  transform: { scaleX: 0.0023, scaleY: -0.0023, translateX: 450, translateY: 380, rotationDeg: 0 },
  trackPath: 'M150,300 L600,300 Z',
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
  vi.resetAllMocks()
  globalThis.fetch = vi.fn()
})

describe('TrackMap', () => {
  it('shows unavailable message when circuitId is null', () => {
    render(<TrackMap circuitId={null} />)
    expect(screen.getByTestId('track-map-unavailable')).toBeInTheDocument()
  })

  it('shows unavailable message when circuit config fetch fails', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({ ok: false } as Response)
    render(<TrackMap circuitId="unknowncircuit" />)
    await waitFor(() =>
      expect(screen.getByTestId('track-map-unavailable')).toBeInTheDocument()
    )
  })

  it('renders SVG when config loads successfully', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockConfig),
    } as Response)
    render(<TrackMap circuitId="monza" />)
    await waitFor(() => expect(screen.getByTestId('track-map-svg')).toBeInTheDocument())
  })

  it('fetches the circuit config from a relative same-origin path, never the backend API base URL', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockConfig),
    } as Response)
    render(<TrackMap circuitId="monza" />)
    await waitFor(() => expect(globalThis.fetch).toHaveBeenCalled())

    const requestedUrl = vi.mocked(globalThis.fetch).mock.calls[0][0]
    expect(requestedUrl).toBe('/circuit-configs/monza.json')
  })

  it('returns null in fallback mode', () => {
    useLiveRaceStore.setState({ sessionMode: 'fallback' })
    const { container } = render(<TrackMap circuitId="monza" />)
    expect(container.firstChild).toBeNull()
  })

  it('renders driver dots for drivers with x/y coordinates', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockConfig),
    } as Response)
    useLiveRaceStore.setState({
      drivers: {
        '1': {
          driverNumber: 1, driverCode: 'VER', teamName: 'Red Bull Racing',
          teamColour: '3671C6', position: 1, gapToCarAhead: null,
          gapIsStale: false, tyreCompound: null, stintLaps: null,
          championshipDelta: null, x: -1500.0, y: 823.0,
          miniSectorStatus: null,
          pitWindowActive: false,
        },
      },
    })
    render(<TrackMap circuitId="monza" />)
    await waitFor(() => expect(screen.getByTestId('track-map-svg')).toBeInTheDocument())
    await waitFor(
      () => expect(screen.getByTestId('driver-dot-VER')).toBeInTheDocument(),
      { timeout: 2000 }
    )
  })

  it('renders purple stroke on driver dot for session-fastest sector', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockConfig),
    } as Response)
    useLiveRaceStore.setState({
      drivers: {
        '1': {
          driverNumber: 1, driverCode: 'VER', teamName: 'Red Bull Racing',
          teamColour: '3671C6', position: 1, gapToCarAhead: null,
          gapIsStale: false, tyreCompound: null, stintLaps: null,
          championshipDelta: null, x: -1500.0, y: 823.0,
          miniSectorStatus: 'purple',
          pitWindowActive: false,
        },
      },
    })
    render(<TrackMap circuitId="monza" />)
    await waitFor(() => expect(screen.getByTestId('driver-dot-VER')).toBeInTheDocument(), { timeout: 2000 })
    const circle = screen.getByTestId('driver-dot-VER').querySelector('circle')
    expect(circle?.getAttribute('stroke')).toBe('#bf00ff')
  })
})
