import { useEffect } from 'react'
import { useRaceSchedule } from '../../shared/api/ergast'
import { useLocalStorage } from '../../shared/hooks/useLocalStorage'
import { useFallbackState } from '../live-race/hooks/useFallbackState'
import { computeNextStreakState, DEFAULT_STREAK_STATE, findCurrentWeekendIndex, STREAK_STORAGE_KEY } from './streakStorage'
import type { StreakState } from './streakStorage'

// Used inside LiveRacePage — side-effect only, no rendered output. Recording
// happens here (where `isLive` is meaningfully observed via the SignalR
// connection this page owns), while StreakCounter (on the calendar page)
// only ever reads the already-persisted count.
export function useRecordLiveVisit(): void {
  const { isLive } = useFallbackState()
  const { data: schedule } = useRaceSchedule()
  const [streakState, setStreakState] = useLocalStorage<StreakState>(STREAK_STORAGE_KEY, DEFAULT_STREAK_STATE)

  useEffect(() => {
    if (!isLive || !schedule) return

    const currentIndex = findCurrentWeekendIndex(schedule, new Date())
    if (currentIndex === null) return

    const next = computeNextStreakState(streakState, currentIndex)
    if (next !== streakState) setStreakState(next)
  }, [isLive, schedule, streakState, setStreakState])
}
