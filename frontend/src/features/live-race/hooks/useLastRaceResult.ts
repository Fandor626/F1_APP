import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../../shared/api/queryKeys'
import type { LastRaceResult } from '../../../shared/types/f1'

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

// LastRaceResult | null: the backend returns 204 No Content when no race has
// completed yet this season (e.g. very early season) — a legitimate "there is
// nothing to fetch" outcome, not an error. Resolving to null (rather than
// throwing on the empty body) lets callers distinguish "confirmed no prior
// race" from "still loading" or "request failed".
export function useLastRaceResult(options?: { enabled?: boolean }) {
  return useQuery<LastRaceResult | null>({
    queryKey: queryKeys.lastRaceResult,
    queryFn: async () => {
      const res = await fetch(`${apiBase}/api/races/last-result`)
      if (res.status === 204) return null
      if (!res.ok) throw new Error('Failed to fetch last race result')
      return res.json()
    },
    staleTime: 60 * 60 * 1000,
    enabled: options?.enabled ?? true,
  })
}
