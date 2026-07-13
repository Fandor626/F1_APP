import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ReplayBar } from './ReplayBar'
import { useReplayStore } from '../store/replayStore'
import { useLiveRaceStore } from '../store/liveRaceStore'
import { useRaceReplayQuery } from '../hooks/useRaceReplayQuery'
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
  // clearAllMocks() resets call history but NOT a mock's configured return
  // value (that needs resetAllMocks/resetHandlers) — without re-establishing
  // this default, a later test's mockReturnValue([...]) leaks into whichever
  // test runs next, since it's the same module-mocked function instance.
  vi.mocked(useRaceReplayQuery).mockReturnValue({ data: undefined } as never)
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

  it('is not rendered until replay frames have loaded', () => {
    render(<ReplayBar season={2026} round={16} />)

    expect(screen.queryByTestId('replay-scrub-bar')).not.toBeInTheDocument()
  })

  it('renders a discrete (step=1) slider spanning every lap once frames are loaded', async () => {
    const { useRaceReplayQuery } = await import('../hooks/useRaceReplayQuery')
    vi.mocked(useRaceReplayQuery).mockReturnValue({
      data: [makeFrame(1, 44), makeFrame(2, 44), makeFrame(3, 44)],
    } as never)

    render(<ReplayBar season={2026} round={16} />)

    const scrub = await screen.findByTestId('replay-scrub-bar')
    expect(scrub).toHaveAttribute('type', 'range')
    expect(scrub).toHaveAttribute('min', '0')
    expect(scrub).toHaveAttribute('max', '2') // 3 frames → indices 0..2
    expect(scrub).toHaveAttribute('step', '1')
    // Implicit ARIA role from the native element — this IS the "role=slider"
    // requirement (UX-DR4), not a separate attribute to add.
    expect(scrub).toEqual(screen.getByRole('slider'))
  })

  it('scrubbing jumps to the selected lap and updates every dependent view immediately', async () => {
    const { useRaceReplayQuery } = await import('../hooks/useRaceReplayQuery')
    vi.mocked(useRaceReplayQuery).mockReturnValue({
      data: [makeFrame(1, 44), makeFrame(2, 44), makeFrame(3, 44)],
    } as never)

    render(<ReplayBar season={2026} round={16} />)
    const scrub = await screen.findByTestId('replay-scrub-bar')

    fireEvent.change(scrub, { target: { value: '2' } })

    expect(useReplayStore.getState().currentLapIndex).toBe(2)
    await waitFor(() => expect(useLiveRaceStore.getState().drivers['44']?.stintLaps).toBe(3))
    expect(screen.getByTestId('replay-lap-readout')).toHaveTextContent('Lap 3 / 3')
  })

  it.each([true, false])('scrubbing never changes isPlaying (was %s)', async (initialIsPlaying) => {
    const { useRaceReplayQuery } = await import('../hooks/useRaceReplayQuery')
    vi.mocked(useRaceReplayQuery).mockReturnValue({
      data: [makeFrame(1, 44), makeFrame(2, 44)],
    } as never)
    useReplayStore.setState({ isPlaying: initialIsPlaying })

    render(<ReplayBar season={2026} round={16} />)
    const scrub = await screen.findByTestId('replay-scrub-bar')

    fireEvent.change(scrub, { target: { value: '1' } })

    expect(useReplayStore.getState().isPlaying).toBe(initialIsPlaying)
  })

  // Note: jsdom does not simulate native <input type="range"> keyboard
  // stepping (verified directly — `fireEvent.keyDown` with ArrowRight/Home/
  // End leaves `.value` unchanged in this test environment, unlike real
  // browsers). AC 4's Left/Right/Home/End behavior is real, standard HTML
  // range-input semantics (verified against the HTML spec, not custom code —
  // there is no bespoke keydown handler here to test), but this suite cannot
  // exercise it directly. What IS verified above: the element is a real
  // native range input with the correct min/max/step, which is exactly what
  // makes that native keyboard behavior apply in an actual browser.

  it('defaults to 1x speed, visually indicated as active', () => {
    render(<ReplayBar season={2026} round={16} />)

    expect(screen.getByTestId('replay-speed-1x')).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByTestId('replay-speed-2x')).toHaveAttribute('aria-selected', 'false')
    expect(screen.getByTestId('replay-speed-4x')).toHaveAttribute('aria-selected', 'false')
  })

  it('selecting a speed updates the store and the active indicator, without resetting the lap', () => {
    useReplayStore.setState({ currentLapIndex: 3, isPlaying: true })
    render(<ReplayBar season={2026} round={16} />)

    fireEvent.click(screen.getByTestId('replay-speed-4x'))

    expect(useReplayStore.getState().speed).toBe(4)
    expect(screen.getByTestId('replay-speed-4x')).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByTestId('replay-speed-1x')).toHaveAttribute('aria-selected', 'false')
    // AC 1: "advances at that rate without restarting" — lap position and
    // play state are untouched by a speed change.
    expect(useReplayStore.getState().currentLapIndex).toBe(3)
    expect(useReplayStore.getState().isPlaying).toBe(true)
  })

  it('the mobile overflow toggle reveals and hides Restart + the speed group', () => {
    render(<ReplayBar season={2026} round={16} />)

    const toggle = screen.getByTestId('replay-overflow-toggle')
    expect(toggle).toHaveAttribute('aria-expanded', 'false')

    fireEvent.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'true')

    fireEvent.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
  })

  // Note: which of "always visible" (desktop) vs. "hidden until the overflow
  // toggle is open" (mobile) actually applies is decided by the md: CSS
  // breakpoint at real render time — jsdom has no viewport to evaluate that
  // against, so this suite can't assert which state wins at a given width.
  // What IS verified above: Restart and the speed group are present exactly
  // once each (no duplicated markup/test ids across a desktop/mobile split),
  // and the overflow toggle's own open/close state genuinely works.
})
