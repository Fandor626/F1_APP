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
  weekendStart: z.string(),
  raceStart: z.string(),
})

const RaceScheduleSchema = z.array(RaceWeekendSchema)

export type RaceWeekend = z.infer<typeof RaceWeekendSchema>

const DriverStandingSchema = z.object({
  position: z.number(),
  driverName: z.string(),
  constructorName: z.string(),
  points: z.number(),
})

const DriverStandingsSchema = z.array(DriverStandingSchema)

export type DriverStanding = z.infer<typeof DriverStandingSchema>

const ConstructorStandingSchema = z.object({
  position: z.number(),
  constructorName: z.string(),
  points: z.number(),
})

const ConstructorStandingsSchema = z.array(ConstructorStandingSchema)

export type ConstructorStanding = z.infer<typeof ConstructorStandingSchema>

const SessionSchema = z.object({
  name: z.string(),
  start: z.string(),
})

const PriorYearWinnerSchema = z.object({
  driverName: z.string(),
  constructorName: z.string(),
  time: z.string().optional(),
})

const ChampionshipDeltaSchema = z.object({
  leaderName: z.string(),
  runnerUpName: z.string(),
  pointsGap: z.number(),
})

const RaceWeekendDetailSchema = z.object({
  season: z.number(),
  round: z.number(),
  raceName: z.string(),
  circuitName: z.string(),
  country: z.string(),
  sessions: z.array(SessionSchema),
  priorYearWinner: PriorYearWinnerSchema.optional(),
  championshipDelta: ChampionshipDeltaSchema.optional(),
})

export type Session = z.infer<typeof SessionSchema>
export type PriorYearWinner = z.infer<typeof PriorYearWinnerSchema>
export type ChampionshipDelta = z.infer<typeof ChampionshipDeltaSchema>
export type RaceWeekendDetail = z.infer<typeof RaceWeekendDetailSchema>

const WinProbabilityEntrySchema = z.object({
  driverName: z.string(),
  constructorName: z.string(),
  gridPosition: z.number(),
  winProbability: z.number(),
})

const WinProbabilitySchema = z.array(WinProbabilityEntrySchema)

export type WinProbabilityEntry = z.infer<typeof WinProbabilityEntrySchema>

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined

// Mirrors backend's schedule cache TTL — no point polling more often than
// the data can actually change.
const RACE_SCHEDULE_STALE_TIME_MS = 1000 * 60 * 60

// Mirrors backend's standings cache TTL (1h).
const STANDINGS_STALE_TIME_MS = 1000 * 60 * 60

// Mirrors backend's qualifying results cache TTL (6h).
const QUALIFYING_STALE_TIME_MS = 1000 * 60 * 60 * 6

// A hung connection (no response, no error) must not leave the page stuck
// on a loading state forever — see deferred-work.md from Story 1.1.
const REQUEST_TIMEOUT_MS = 10_000

async function fetchJson<T>(path: string, schema: z.ZodType<T>, querySignal: AbortSignal): Promise<T> {
  if (!API_BASE_URL) {
    throw new Error('VITE_API_BASE_URL is not set — copy .env.example to .env.local')
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    signal: AbortSignal.any([querySignal, AbortSignal.timeout(REQUEST_TIMEOUT_MS)]),
  })

  if (!response.ok) {
    throw new Error(`Request to ${path} failed: ${response.status}`)
  }

  return schema.parse(await response.json())
}

export function useRaceSchedule() {
  return useQuery({
    queryKey: queryKeys.races,
    queryFn: ({ signal }) => fetchJson('/api/races', RaceScheduleSchema, signal),
    staleTime: RACE_SCHEDULE_STALE_TIME_MS,
    retry: false,
  })
}

export function useDriverStandings() {
  return useQuery({
    queryKey: queryKeys.standings.drivers,
    queryFn: ({ signal }) => fetchJson('/api/standings/drivers', DriverStandingsSchema, signal),
    staleTime: STANDINGS_STALE_TIME_MS,
    retry: false,
  })
}

export function useConstructorStandings() {
  return useQuery({
    queryKey: queryKeys.standings.constructors,
    queryFn: ({ signal }) => fetchJson('/api/standings/constructors', ConstructorStandingsSchema, signal),
    staleTime: STANDINGS_STALE_TIME_MS,
    retry: false,
  })
}

export function useRaceDetail(round: number) {
  return useQuery({
    queryKey: queryKeys.raceDetail(round),
    queryFn: ({ signal }) => fetchJson(`/api/races/${round}`, RaceWeekendDetailSchema, signal),
    staleTime: RACE_SCHEDULE_STALE_TIME_MS,
    retry: false,
  })
}

export function useWinProbability(round: number) {
  return useQuery({
    queryKey: queryKeys.winProbability(round),
    queryFn: ({ signal }) => fetchJson(`/api/races/${round}/win-probability`, WinProbabilitySchema, signal),
    staleTime: QUALIFYING_STALE_TIME_MS,
    enabled: !isNaN(round) && round > 0,
    retry: false,
  })
}
