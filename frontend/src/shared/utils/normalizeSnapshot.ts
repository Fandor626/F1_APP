import type { DriverState } from '../types/f1'

export function normalizeSnapshot(drivers: DriverState[]): Record<string, DriverState> {
  return Object.fromEntries(drivers.map(d => [String(d.driverNumber), d]))
}
