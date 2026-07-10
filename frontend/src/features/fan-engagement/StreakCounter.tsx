import { useLocalStorage } from '../../shared/hooks/useLocalStorage'
import { DEFAULT_STREAK_STATE, STREAK_STORAGE_KEY } from './streakStorage'
import type { StreakState } from './streakStorage'

export function StreakCounter() {
  const [{ count }] = useLocalStorage<StreakState>(STREAK_STORAGE_KEY, DEFAULT_STREAK_STATE)

  // A fan who has never watched live has no streak to show — an empty/zero
  // badge would be noise, not information.
  if (count === 0) return null

  return (
    <span
      data-testid="streak-counter"
      className="inline-flex items-center gap-1.5 rounded-full border border-border-soft bg-bg-inset px-3 py-1 text-[11.5px] font-semibold tracking-[0.04em] text-text-secondary"
    >
      Streak <b className="text-accent-editorial">{count}</b>
    </span>
  )
}
