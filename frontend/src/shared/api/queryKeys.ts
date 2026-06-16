export const queryKeys = {
  races: ['races', 'current'] as const,
  standings: {
    drivers: ['standings', 'drivers', 'current'] as const,
    constructors: ['standings', 'constructors', 'current'] as const,
  },
}
