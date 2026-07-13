import { useEffect, useState } from 'react'
import { useReplayStore } from '../store/replayStore'
import { useRaceReplayQuery } from '../hooks/useRaceReplayQuery'
import { useLiveRaceStore } from '../store/liveRaceStore'
import { normalizeSnapshot } from '../../../shared/utils/normalizeSnapshot'

// Base tick interval at 1x speed; actual interval is BASE_TICK_MS / speed
// (Architecture AD-4). No source doc pins an exact value — 3s/lap is a
// reasonable default pace for skimming a replay, not a precision requirement.
const BASE_TICK_MS = 3000

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

  const [hasStarted, setHasStarted] = useState(false)
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
      <button
        type="button"
        onClick={restart}
        data-testid="replay-restart"
        aria-label="Restart"
        className="flex h-11 w-11 items-center justify-center rounded-full border-2 border-[#2a2f38] text-[16px] text-[#9aa1ad]"
      >
        ⟲
      </button>
      <span className="text-[13px] tabular-nums text-[#eef0f3]" data-testid="replay-lap-readout">
        Lap {currentLapIndex + 1} / {totalLaps || '–'}
      </span>
    </div>
  )
}
