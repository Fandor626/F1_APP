import { describe, expect, it } from 'vitest'
import { computeNextStreakState, findCurrentWeekendIndex } from './streakStorage'
import type { RaceWeekend } from '../../shared/api/ergast'

function race(round: number, weekendStart: string, raceStart: string): RaceWeekend {
  return {
    season: 2026,
    round,
    raceName: `Race ${round}`,
    circuitId: `circuit-${round}`,
    circuitName: `Circuit ${round}`,
    locality: 'City',
    country: 'Country',
    weekendStart,
    raceStart,
  }
}

const schedule: RaceWeekend[] = [
  race(1, '2026-03-06T10:00:00Z', '2026-03-08T15:00:00Z'),
  race(2, '2026-03-13T10:00:00Z', '2026-03-15T15:00:00Z'),
  race(3, '2026-03-20T10:00:00Z', '2026-03-22T15:00:00Z'),
]

describe('findCurrentWeekendIndex', () => {
  it('finds the race whose window contains now', () => {
    const now = new Date('2026-03-15T16:00:00Z') // during round 2's race day
    expect(findCurrentWeekendIndex(schedule, now)).toBe(1)
  })

  it('includes a ~24h buffer after raceStart', () => {
    const now = new Date('2026-03-08T20:00:00Z') // a few hours after round 1's raceStart
    expect(findCurrentWeekendIndex(schedule, now)).toBe(0)
  })

  it('returns null when now matches no weekend window', () => {
    const now = new Date('2026-01-01T00:00:00Z')
    expect(findCurrentWeekendIndex(schedule, now)).toBeNull()
  })
})

describe('computeNextStreakState', () => {
  it('starts the streak at 1 on the very first counted weekend', () => {
    const next = computeNextStreakState({ count: 0, lastCountedIndex: null }, 0)
    expect(next).toEqual({ count: 1, lastCountedIndex: 0 })
  })

  it('is idempotent for the same weekend — calling it again does not increment', () => {
    const first = computeNextStreakState({ count: 0, lastCountedIndex: null }, 0)
    const second = computeNextStreakState(first, 0)
    expect(second).toEqual({ count: 1, lastCountedIndex: 0 })
  })

  it('increments the count for a consecutive weekend', () => {
    const state = { count: 3, lastCountedIndex: 4 }
    const next = computeNextStreakState(state, 5)
    expect(next).toEqual({ count: 4, lastCountedIndex: 5 })
  })

  it('resets the count to 1 when a weekend was skipped', () => {
    const state = { count: 3, lastCountedIndex: 4 }
    const next = computeNextStreakState(state, 6) // skipped weekend 5
    expect(next).toEqual({ count: 1, lastCountedIndex: 6 })
  })
})
