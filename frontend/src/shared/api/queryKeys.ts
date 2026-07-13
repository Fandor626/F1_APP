export const queryKeys = {
  races: ['races', 'current'] as const,
  raceDetail: (round: number) => ['races', 'detail', round] as const,
  winProbability: (round: number) => ['races', 'win-probability', round] as const,
  lastRaceResult: ['races', 'last-result'] as const,
  raceReplay: (season: number, round: number) => ['races', 'replay', season, round] as const,
  circuitProfile: (circuitId: string) => ['circuits', 'profile', circuitId] as const,
  driverProfile: (driverId: string) => ['drivers', 'profile', driverId] as const,
  allDrivers: ['drivers', 'all'] as const,
  headToHead: (driverA: string, driverB: string, season: number | null, circuitId: string | null) =>
    ['drivers', 'compare', driverA, driverB, season, circuitId] as const,
  news: ['news', 'feed'] as const,
  standings: {
    drivers: ['standings', 'drivers', 'current'] as const,
    constructors: ['standings', 'constructors', 'current'] as const,
    trajectory: ['standings', 'trajectory', 'current'] as const,
    seasonWrapped: ['standings', 'season-wrapped', 'current'] as const,
  },
}
