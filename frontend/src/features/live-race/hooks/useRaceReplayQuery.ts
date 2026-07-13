import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../../shared/api/queryKeys'
import type { RaceStateSnapshot } from '../../../shared/types/f1'

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

interface UseRaceReplayQueryOptions {
  season: number | null
  round: number | null
  enabled: boolean
}

// Fetches the full per-lap replay frame array once (Architecture AD-2) — not
// per-lap, not on every scrub. The array itself stays in TanStack Query
// cache; replayStore only ever holds a currentLapIndex pointer into it.
export function useRaceReplayQuery({ season, round, enabled }: UseRaceReplayQueryOptions) {
  return useQuery<RaceStateSnapshot[]>({
    queryKey: queryKeys.raceReplay(season ?? -1, round ?? -1),
    queryFn: async () => {
      const res = await fetch(`${apiBase}/api/races/${season}/${round}/replay`)
      if (!res.ok) throw new Error('Failed to fetch race replay')
      return res.json()
    },
    staleTime: 60 * 60 * 1000,
    enabled: enabled && season !== null && round !== null,
  })
}
