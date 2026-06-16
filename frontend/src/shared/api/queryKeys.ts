export const queryKeys = {
  races: ['races', 'current'] as const,
  raceDetail: (round: number) => ['races', 'detail', round] as const,
  standings: {
    drivers: ['standings', 'drivers', 'current'] as const,
    constructors: ['standings', 'constructors', 'current'] as const,
  },
}
