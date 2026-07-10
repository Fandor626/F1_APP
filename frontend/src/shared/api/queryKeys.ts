export const queryKeys = {
  races: ['races', 'current'] as const,
  raceDetail: (round: number) => ['races', 'detail', round] as const,
  winProbability: (round: number) => ['races', 'win-probability', round] as const,
  lastRaceResult: ['races', 'last-result'] as const,
  standings: {
    drivers: ['standings', 'drivers', 'current'] as const,
    constructors: ['standings', 'constructors', 'current'] as const,
    trajectory: ['standings', 'trajectory', 'current'] as const,
  },
}
