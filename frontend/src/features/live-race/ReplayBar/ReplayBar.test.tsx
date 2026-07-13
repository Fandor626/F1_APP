import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ReplayBar } from './ReplayBar'
import { useReplayStore } from '../store/replayStore'
import { useLiveRaceStore } from '../store/liveRaceStore'
import type { RaceStateSnapshot } from '../../../shared/types/f1'

vi.mock('../hooks/useRaceReplayQuery', () => ({
  useRaceReplayQuery: vi.fn().mockReturnValue({ data: undefined }),
}))

function makeFrame(lapNumber: number, driverNumber: number): RaceStateSnapshot {
  return {
    capturedAt: '2026-09-07T13:00:00Z',
    drivers: [
      {
        driverNumber, driverCode: `D${lapNumber}`, teamName: 'Team', teamColour: 'FF0000',
        position: 1, gapToCarAhead: null, gapIsStale: false, tyreCompound: 'MEDIUM',
        stintLaps: lapNumber, championshipDelta: null, x: null, y: null,
        miniSectorStatus: null, pitWindowActive: false,
      },
    ],
    lapChart: {},
    sessionMode: 'fallback',
    fallbackRaceName: 'Italian Grand Prix',
    circuitId: null,
    fastestSectors: null,
    timeline: [],
  }
}

beforeEach(() => {
  useReplayStore.setState({ currentLapIndex: 0, isPlaying: false, speed: 1 })
  useLiveRaceStore.setState({ drivers: {}, lapChart: {}, fastestSectors: null, timeline: [] })
  vi.clearAllMocks()
})

describe('ReplayBar', () => {
  it('renders play/pause and restart controls with a lap readout', () => {
    render(<ReplayBar season={2026} round={16} />)

    expect(screen.getByTestId('replay-play-pause')).toBeInTheDocument()
    expect(screen.getByTestId('replay-restart')).toBeInTheDocument()
    expect(screen.getByTestId('replay-lap-readout')).toHaveTextContent('Lap 1 / –')
  })

  it('toggles the play/pause button label when clicked', () => {
    render(<ReplayBar season={2026} round={16} />)

    const button = screen.getByTestId('replay-play-pause')
    expect(button).toHaveAttribute('aria-label', 'Play')

    fireEvent.click(button)
    expect(button).toHaveAttribute('aria-label', 'Pause')

    fireEvent.click(button)
    expect(button).toHaveAttribute('aria-label', 'Play')
  })

  it('restart resets currentLapIndex to 0 without a page reload', () => {
    useReplayStore.setState({ currentLapIndex: 5 })
    render(<ReplayBar season={2026} round={16} />)

    fireEvent.click(screen.getByTestId('replay-restart'))

    expect(useReplayStore.getState().currentLapIndex).toBe(0)
  })

  it('applies the current frame to the shared liveRaceStore via the full setter sequence', async () => {
    const { useRaceReplayQuery } = await import('../hooks/useRaceReplayQuery')
    vi.mocked(useRaceReplayQuery).mockReturnValue({
      data: [makeFrame(1, 44), makeFrame(2, 44)],
    } as never)

    render(<ReplayBar season={2026} round={16} />)

    await waitFor(() => {
      const { drivers } = useLiveRaceStore.getState()
      expect(drivers['44']).toBeDefined()
      expect(drivers['44']?.stintLaps).toBe(1)
    })

    expect(screen.getByTestId('replay-lap-readout')).toHaveTextContent('Lap 1 / 2')
  })

  it('advancing currentLapIndex updates the applied frame', async () => {
    const { useRaceReplayQuery } = await import('../hooks/useRaceReplayQuery')
    vi.mocked(useRaceReplayQuery).mockReturnValue({
      data: [makeFrame(1, 44), makeFrame(2, 44)],
    } as never)

    render(<ReplayBar season={2026} round={16} />)
    await waitFor(() => expect(useLiveRaceStore.getState().drivers['44']?.stintLaps).toBe(1))

    useReplayStore.getState().setCurrentLapIndex(1)

    await waitFor(() => expect(useLiveRaceStore.getState().drivers['44']?.stintLaps).toBe(2))
  })
})
