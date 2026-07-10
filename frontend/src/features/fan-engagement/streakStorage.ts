import type { RaceWeekend } from '../../shared/api/ergast'

export const STREAK_STORAGE_KEY = 'f1app__streak__v1'

export interface StreakState {
  count: number
  lastCountedIndex: number | null
}

export const DEFAULT_STREAK_STATE: StreakState = { count: 0, lastCountedIndex: null }

// A weekend is "current" if `now` falls between its weekend start and ~1 day
// after its race — covers the live session window with a buffer for
// late-running sessions, without needing a live-race-specific round/season
// signal (none exists in liveRaceStore today).
export function findCurrentWeekendIndex(races: RaceWeekend[], now: Date): number | null {
  const index = races.findIndex((race) => {
    const start = new Date(race.weekendStart)
    const end = new Date(new Date(race.raceStart).getTime() + 24 * 60 * 60 * 1000)
    return now >= start && now <= end
  })
  return index === -1 ? null : index
}

// Pure — safe to call on every render while a live session is active; a
// weekend already counted is a no-op (AC 1: "once per weekend"), a gap
// resets the streak (AC 2), and adjacency continues it.
export function computeNextStreakState(current: StreakState, currentIndex: number): StreakState {
  if (current.lastCountedIndex === currentIndex) return current // already counted this weekend

  const isConsecutive = current.lastCountedIndex !== null && currentIndex === current.lastCountedIndex + 1
  return {
    count: isConsecutive ? current.count + 1 : 1,
    lastCountedIndex: currentIndex,
  }
}
