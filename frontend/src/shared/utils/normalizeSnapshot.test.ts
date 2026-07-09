import { describe, it, expect } from 'vitest'
import { normalizeSnapshot } from './normalizeSnapshot'
import type { DriverState } from '../types/f1'

function makeDriver(driverNumber: number, position: number): DriverState {
  return {
    driverNumber,
    driverCode: String(driverNumber),
    teamName: '',
    teamColour: '555555',
    position,
    gapToCarAhead: null,
    gapIsStale: false,
    tyreCompound: null,
    stintLaps: null,
    championshipDelta: null,
    x: null,
    y: null,
    miniSectorStatus: null,
  }
}

describe('normalizeSnapshot', () => {
  it('returns empty record for empty array', () => {
    expect(normalizeSnapshot([])).toEqual({})
  })

  it('keys three drivers by driverNumber string', () => {
    const drivers = [makeDriver(1, 1), makeDriver(33, 2), makeDriver(44, 3)]
    const result = normalizeSnapshot(drivers)

    expect(Object.keys(result)).toHaveLength(3)
    expect(result['1'].position).toBe(1)
    expect(result['33'].position).toBe(2)
    expect(result['44'].position).toBe(3)
  })

  it('last entry wins for duplicate driver number', () => {
    const first = makeDriver(1, 1)
    const second = { ...makeDriver(1, 5) }
    const result = normalizeSnapshot([first, second])

    expect(Object.keys(result)).toHaveLength(1)
    expect(result['1'].position).toBe(5)
  })
})
