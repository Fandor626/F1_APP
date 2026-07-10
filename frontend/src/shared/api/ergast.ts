import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { queryKeys } from './queryKeys'

const RaceWeekendSchema = z.object({
  season: z.number(),
  round: z.number(),
  raceName: z.string(),
  circuitId: z.string(),
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
  driverId: z.string(),
  driverName: z.string(),
  constructorName: z.string(),
  points: z.number(),
  wins: z.number(),
  nationality: z.string(),
})

const DriverStandingsSchema = z.array(DriverStandingSchema)

export type DriverStanding = z.infer<typeof DriverStandingSchema>

const ConstructorStandingSchema = z.object({
  position: z.number(),
  constructorName: z.string(),
  points: z.number(),
  wins: z.number(),
  nationality: z.string(),
})

const ConstructorStandingsSchema = z.array(ConstructorStandingSchema)

export type ConstructorStanding = z.infer<typeof ConstructorStandingSchema>

const TrajectoryPointSchema = z.object({
  round: z.number(),
  raceName: z.string(),
  resultPosition: z.number().nullable(),
  pointsThisRound: z.number(),
  cumulativePoints: z.number(),
})

const DriverTrajectorySchema = z.object({
  driverId: z.string(),
  driverName: z.string(),
  constructorName: z.string(),
  points: z.array(TrajectoryPointSchema),
})

const TrajectoriesSchema = z.array(DriverTrajectorySchema)

export type TrajectoryPoint = z.infer<typeof TrajectoryPointSchema>
export type DriverTrajectory = z.infer<typeof DriverTrajectorySchema>

const DramaticRaceAwardSchema = z.object({
  raceName: z.string(),
  round: z.number(),
  totalPositionSwing: z.number(),
})

const DriverStatAwardSchema = z.object({
  driverId: z.string(),
  driverName: z.string(),
  constructorName: z.string(),
  value: z.number(),
})

const DriverRaceAwardSchema = z.object({
  driverId: z.string(),
  driverName: z.string(),
  constructorName: z.string(),
  raceName: z.string(),
  positionsGained: z.number(),
})

const ConstructorImprovementAwardSchema = z.object({
  constructorName: z.string(),
  earlySeasonPosition: z.number(),
  finalPosition: z.number(),
  positionsImproved: z.number(),
})

const SeasonWrappedSchema = z
  .object({
    mostDramaticRace: DramaticRaceAwardSchema,
    mostDnfs: DriverStatAwardSchema,
    biggestPointsComeback: DriverStatAwardSchema,
    mostPositionsGainedInARace: DriverRaceAwardSchema,
    mostImprovedConstructor: ConstructorImprovementAwardSchema,
  })
  .nullable()

export type SeasonWrapped = z.infer<typeof SeasonWrappedSchema>

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
  circuitId: z.string(),
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

// Mirrors backend's historical-results cache TTL (7 days) — circuit profile
// data only changes once per race weekend held there.
const HISTORICAL_STALE_TIME_MS = 1000 * 60 * 60 * 24 * 7

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

// For "profile" endpoints (circuit, driver) where a 404 is a legitimate
// "not found" outcome to render, not an error state.
async function fetchNullable404Json<T>(path: string, schema: z.ZodType<T>, querySignal: AbortSignal): Promise<T | null> {
  if (!API_BASE_URL) {
    throw new Error('VITE_API_BASE_URL is not set — copy .env.example to .env.local')
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    signal: AbortSignal.any([querySignal, AbortSignal.timeout(REQUEST_TIMEOUT_MS)]),
  })

  if (response.status === 404) return null
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

export function useChampionshipTrajectory() {
  return useQuery({
    queryKey: queryKeys.standings.trajectory,
    queryFn: ({ signal }) => fetchJson('/api/standings/trajectory', TrajectoriesSchema, signal),
    staleTime: STANDINGS_STALE_TIME_MS,
    retry: false,
  })
}

export function useSeasonWrapped() {
  return useQuery({
    queryKey: queryKeys.standings.seasonWrapped,
    queryFn: ({ signal }) => fetchJson('/api/standings/season-wrapped', SeasonWrappedSchema, signal),
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

const LapRecordSchema = z.object({
  driverName: z.string(),
  constructorName: z.string(),
  time: z.string(),
  season: z.number(),
})

const CircuitWinnerSchema = z.object({
  season: z.number(),
  driverName: z.string(),
  constructorName: z.string(),
})

const CircuitStatsSchema = z.object({
  lengthKm: z.number(),
  corners: z.number(),
  drsZones: z.number(),
})

const CircuitProfileSchema = z.object({
  circuitId: z.string(),
  circuitName: z.string(),
  locality: z.string(),
  country: z.string(),
  firstF1Season: z.number(),
  lapRecord: LapRecordSchema.nullable(),
  pastWinners: z.array(CircuitWinnerSchema),
  stats: CircuitStatsSchema.nullable(),
})

export type CircuitProfile = z.infer<typeof CircuitProfileSchema>

export function useCircuitProfile(circuitId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.circuitProfile(circuitId ?? ''),
    queryFn: ({ signal }) => fetchNullable404Json(`/api/circuits/${circuitId}`, CircuitProfileSchema, signal),
    staleTime: HISTORICAL_STALE_TIME_MS,
    enabled: !!circuitId,
    retry: false,
  })
}

const DriverCareerTotalsSchema = z.object({
  races: z.number(),
  wins: z.number(),
  podiums: z.number(),
  poles: z.number(),
  fastestLaps: z.number(),
  titles: z.number(),
})

const ConstructorHistoryEntrySchema = z.object({
  season: z.number(),
  constructorNames: z.array(z.string()),
})

const DriverCareerPointSchema = z.object({
  season: z.number(),
  round: z.number(),
  raceName: z.string(),
  pointsThisRound: z.number(),
  cumulativePoints: z.number(),
})

const DriverProfileSchema = z.object({
  driverId: z.string(),
  fullName: z.string(),
  nationality: z.string(),
  careerTotals: DriverCareerTotalsSchema,
  constructorHistory: z.array(ConstructorHistoryEntrySchema),
  careerPoints: z.array(DriverCareerPointSchema),
})

export type DriverProfile = z.infer<typeof DriverProfileSchema>
export type DriverCareerPoint = z.infer<typeof DriverCareerPointSchema>

export function useDriverProfile(driverId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.driverProfile(driverId ?? ''),
    queryFn: ({ signal }) => fetchNullable404Json(`/api/drivers/${driverId}`, DriverProfileSchema, signal),
    staleTime: HISTORICAL_STALE_TIME_MS,
    enabled: !!driverId,
    retry: false,
  })
}

const DriverOptionSchema = z.object({
  driverId: z.string(),
  fullName: z.string(),
})

const DriverOptionsSchema = z.array(DriverOptionSchema)

export type DriverOption = z.infer<typeof DriverOptionSchema>

export function useAllDrivers() {
  return useQuery({
    queryKey: queryKeys.allDrivers,
    queryFn: ({ signal }) => fetchJson('/api/drivers', DriverOptionsSchema, signal),
    staleTime: HISTORICAL_STALE_TIME_MS,
    retry: false,
  })
}

const HeadToHeadDriverStatsSchema = z.object({
  driverId: z.string(),
  fullName: z.string(),
  qualifyingAveragePosition: z.number().nullable(),
  raceFinishAveragePosition: z.number().nullable(),
  dnfCount: z.number(),
  pointsScored: z.number(),
  fastestLaps: z.number(),
  wins: z.number(),
  racesCompared: z.number(),
})

const HeadToHeadComparisonSchema = z
  .object({
    driverA: HeadToHeadDriverStatsSchema,
    driverB: HeadToHeadDriverStatsSchema,
  })
  .nullable()

export type HeadToHeadDriverStats = z.infer<typeof HeadToHeadDriverStatsSchema>
export type HeadToHeadComparison = z.infer<typeof HeadToHeadComparisonSchema>

export function useHeadToHeadComparison(
  driverA: string | null,
  driverB: string | null,
  season: number | null,
  circuitId: string | null,
) {
  const params = new URLSearchParams({ driverA: driverA ?? '', driverB: driverB ?? '' })
  if (season) params.set('season', String(season))
  if (circuitId) params.set('circuitId', circuitId)

  return useQuery({
    queryKey: queryKeys.headToHead(driverA ?? '', driverB ?? '', season, circuitId),
    queryFn: ({ signal }) => fetchNullable404Json(`/api/drivers/compare?${params}`, HeadToHeadComparisonSchema, signal),
    enabled: !!driverA && !!driverB,
    retry: false,
  })
}
