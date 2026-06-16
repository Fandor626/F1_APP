import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { queryKeys } from './queryKeys'

const RaceWeekendSchema = z.object({
  season: z.number(),
  round: z.number(),
  raceName: z.string(),
  circuitName: z.string(),
  locality: z.string(),
  country: z.string(),
  raceStart: z.string(),
})

const RaceScheduleSchema = z.array(RaceWeekendSchema)

export type RaceWeekend = z.infer<typeof RaceWeekendSchema>

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined

// Mirrors backend's 24h schedule cache TTL — no point polling more often
// than the data can actually change.
const RACE_SCHEDULE_STALE_TIME_MS = 1000 * 60 * 60

// A hung connection (no response, no error) must not leave the page stuck
// on a loading state forever — see deferred-work.md from Story 1.1.
const REQUEST_TIMEOUT_MS = 10_000

async function fetchRaceSchedule(querySignal: AbortSignal): Promise<RaceWeekend[]> {
  if (!API_BASE_URL) {
    throw new Error('VITE_API_BASE_URL is not set — copy .env.example to .env.local')
  }

  const response = await fetch(`${API_BASE_URL}/api/races`, {
    signal: AbortSignal.any([querySignal, AbortSignal.timeout(REQUEST_TIMEOUT_MS)]),
  })

  if (!response.ok) {
    throw new Error(`Race schedule request failed: ${response.status}`)
  }

  return RaceScheduleSchema.parse(await response.json())
}

export function useRaceSchedule() {
  return useQuery({
    queryKey: queryKeys.races,
    queryFn: ({ signal }) => fetchRaceSchedule(signal),
    staleTime: RACE_SCHEDULE_STALE_TIME_MS,
    retry: false,
  })
}
