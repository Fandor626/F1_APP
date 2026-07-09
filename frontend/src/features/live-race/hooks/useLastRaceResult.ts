import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../../shared/api/queryKeys'
import type { LastRaceResult } from '../../../shared/types/f1'

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

export function useLastRaceResult(options?: { enabled?: boolean }) {
  return useQuery<LastRaceResult>({
    queryKey: queryKeys.lastRaceResult,
    queryFn: async () => {
      const res = await fetch(`${apiBase}/api/races/last-result`)
      if (!res.ok) throw new Error('Failed to fetch last race result')
      return res.json()
    },
    staleTime: 60 * 60 * 1000,
    enabled: options?.enabled ?? true,
  })
}
