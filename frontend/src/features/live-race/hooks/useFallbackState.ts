import { useLiveRaceStore } from '../store/liveRaceStore'

export function useFallbackState() {
  const sessionMode = useLiveRaceStore(s => s.sessionMode)
  const fallbackRaceName = useLiveRaceStore(s => s.fallbackRaceName)

  return {
    sessionMode,
    fallbackRaceName,
    isFallback: sessionMode === 'fallback',
    isStale: sessionMode === 'stale',
    isLive: sessionMode === 'live',
  }
}
