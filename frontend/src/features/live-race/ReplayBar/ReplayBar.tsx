import { useEffect, useState } from 'react'
import { useReplayStore } from '../store/replayStore'
import { useRaceReplayQuery } from '../hooks/useRaceReplayQuery'
import { useLiveRaceStore } from '../store/liveRaceStore'
import { normalizeSnapshot } from '../../../shared/utils/normalizeSnapshot'

// Base tick interval at 1x speed; actual interval is BASE_TICK_MS / speed
// (Architecture AD-4). No source doc pins an exact value — 3s/lap is a
// reasonable default pace for skimming a replay, not a precision requirement.
const BASE_TICK_MS = 3000

const SPEEDS = [1, 2, 4] as const

interface ReplayBarProps {
  season: number
  round: number
}

export function ReplayBar({ season, round }: ReplayBarProps) {
  const currentLapIndex = useReplayStore(s => s.currentLapIndex)
  const isPlaying = useReplayStore(s => s.isPlaying)
  const speed = useReplayStore(s => s.speed)
  const play = useReplayStore(s => s.play)
  const pause = useReplayStore(s => s.pause)
  const restart = useReplayStore(s => s.restart)
  const setSpeed = useReplayStore(s => s.setSpeed)

  const [hasStarted, setHasStarted] = useState(false)
  // Mobile-only: Restart + speed group start hidden behind the "⋯" overflow
  // button (DESIGN.md replay-bar.mobile) — irrelevant on desktop, where
  // they're always visible regardless of this flag (see the md:flex override
  // on the wrapper below).
  const [overflowOpen, setOverflowOpen] = useState(false)
  const { data: frames } = useRaceReplayQuery({ season, round, enabled: hasStarted })

  const setDrivers = useLiveRaceStore(s => s.setDrivers)
  const setLapChart = useLiveRaceStore(s => s.setLapChart)
  const setFastestSectors = useLiveRaceStore(s => s.setFastestSectors)
  const setTimeline = useLiveRaceStore(s => s.setTimeline)

  // Applies the current frame to the shared live-race store (Architecture
  // AD-1) whenever it changes — GapList/FastestSectorBoard/LapTimeChart/
  // RaceEventTimeline read from liveRaceStore exactly as they do for a live
  // SignalR snapshot, with no isReplay branching anywhere in those components.
  useEffect(() => {
    const frame = frames?.[currentLapIndex]
    if (!frame) return
    setDrivers(normalizeSnapshot(frame.drivers))
    setLapChart(frame.lapChart)
    setFastestSectors(frame.fastestSectors)
    setTimeline(frame.timeline)
  }, [frames, currentLapIndex, setDrivers, setLapChart, setFastestSectors, setTimeline])

  // Client-side ticking only (AD-4) — no new real-time channel. Reads fresh
  // state from the store on every tick rather than closing over stale
  // values, so a scrub (future story) is always reflected on the next tick.
  useEffect(() => {
    if (!isPlaying || !frames) return

    const interval = window.setInterval(() => {
      const store = useReplayStore.getState()
      const nextIndex = store.currentLapIndex + 1
      if (nextIndex >= frames.length) {
        store.pause()
        return
      }
      store.setCurrentLapIndex(nextIndex)
    }, BASE_TICK_MS / speed)

    return () => window.clearInterval(interval)
  }, [isPlaying, frames, speed])

  function handlePlayPause() {
    if (!hasStarted) setHasStarted(true)
    if (isPlaying) pause()
    else play()
  }

  // Scrubbing only ever sets the index — it never touches isPlaying, so a
  // scrub during playback keeps playing from the new lap and a scrub while
  // paused stays paused (AD-4, AC 3). This is the existing replayStore
  // action from Story 8.2, unchanged — no new store logic needed here.
  function handleScrub(event: React.ChangeEvent<HTMLInputElement>) {
    useReplayStore.getState().setCurrentLapIndex(Number(event.target.value))
  }

  const totalLaps = frames?.length ?? 0

  return (
    <div
      className="fixed bottom-0 left-0 right-0 flex h-[76px] items-center gap-4 border-t border-[#2a2f38] bg-[#1b1f26] px-4"
      data-testid="replay-bar"
    >
      <button
        type="button"
        onClick={handlePlayPause}
        data-testid="replay-play-pause"
        aria-label={isPlaying ? 'Pause' : 'Play'}
        className="flex h-11 w-11 items-center justify-center rounded-full border-2 border-[#d8b65c] text-[16px] text-[#d8b65c]"
      >
        {isPlaying ? '⏸' : '▶'}
      </button>
      {/* Mobile-only trigger for the overflow group below — nothing to
          toggle on desktop, where that group is always visible. */}
      <button
        type="button"
        onClick={() => setOverflowOpen(v => !v)}
        data-testid="replay-overflow-toggle"
        aria-label="More controls"
        aria-expanded={overflowOpen}
        className="flex h-11 w-11 items-center justify-center rounded-full border-2 border-[#2a2f38] text-[16px] text-[#9aa1ad] md:hidden"
      >
        ⋯
      </button>

      {/* Restart + speed group: on mobile, visibility follows overflowOpen;
          on desktop, md:flex unconditionally overrides that and always shows
          it (DESIGN.md replay-bar.mobile — only play/pause + scrub stay
          inline on mobile, everything else moves behind the overflow). One
          copy of these controls, not two — the class expression handles both
          breakpoints without duplicating markup or test ids. */}
      <div className={`items-center gap-3 ${overflowOpen ? 'flex' : 'hidden'} md:flex`}>
        <button
          type="button"
          onClick={restart}
          data-testid="replay-restart"
          aria-label="Restart"
          className="flex h-11 w-11 items-center justify-center rounded-full border-2 border-[#2a2f38] text-[16px] text-[#9aa1ad]"
        >
          ⟲
        </button>
        <div role="tablist" aria-label="Playback speed" className="inline-flex rounded-md border border-[#2a2f38] bg-[#1b1f26] p-[3px]">
          {SPEEDS.map(s => (
            <button
              key={s}
              type="button"
              role="tab"
              aria-selected={speed === s}
              onClick={() => setSpeed(s)}
              data-testid={`replay-speed-${s}x`}
              className={`rounded px-3 py-1.5 text-[12px] font-semibold transition-colors ${
                speed === s ? 'bg-[#20242c] text-[#eef0f3]' : 'text-[#9aa1ad] hover:text-[#eef0f3]'
              }`}
            >
              {s}x
            </button>
          ))}
        </div>
      </div>
      <span className="text-[13px] tabular-nums text-[#eef0f3]" data-testid="replay-lap-readout">
        Lap {currentLapIndex + 1} / {totalLaps || '–'}
      </span>
      {totalLaps > 0 && (
        <>
          {/* Native range input: role="slider", aria-valuenow/min/max, and
              Left/Right/Home/End keyboard stepping are all implicit browser
              behavior — no custom keydown handling needed (UX-DR4). step={1}
              with a totalLaps-1 max makes every value a discrete lap index,
              never a continuous/sub-lap position (AC 2). The datalist below
              renders native tick marks at each lap. */}
          <input
            type="range"
            min={0}
            max={totalLaps - 1}
            step={1}
            value={currentLapIndex}
            onChange={handleScrub}
            list="replay-lap-ticks"
            aria-label="Replay lap scrub bar"
            data-testid="replay-scrub-bar"
            className="h-2 flex-1 accent-[#d8b65c]"
          />
          <datalist id="replay-lap-ticks">
            {Array.from({ length: totalLaps }, (_, i) => (
              <option key={i} value={i} />
            ))}
          </datalist>
        </>
      )}
    </div>
  )
}
