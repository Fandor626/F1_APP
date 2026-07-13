import { create } from 'zustand'

interface ReplayState {
  currentLapIndex: number
  isPlaying: boolean
  speed: 1 | 2 | 4
  setCurrentLapIndex: (index: number) => void
  play: () => void
  pause: () => void
  setSpeed: (speed: 1 | 2 | 4) => void
  restart: () => void
}

// UI-only interaction state (Architecture AD-3) — the fetched replay frame
// array itself lives only in TanStack Query cache (useRaceReplayQuery), never
// here. This store just points into that array.
export const useReplayStore = create<ReplayState>((set) => ({
  currentLapIndex: 0,
  isPlaying: false,
  speed: 1,
  setCurrentLapIndex: (index) => set({ currentLapIndex: index }),
  play: () => set({ isPlaying: true }),
  pause: () => set({ isPlaying: false }),
  setSpeed: (speed) => set({ speed }),
  restart: () => set({ currentLapIndex: 0 }),
}))
