import { beforeEach, describe, expect, it, vi } from 'vitest'

const STORAGE_KEY = 'f1app__fanCard__v1'

beforeEach(() => {
  window.localStorage.clear()
  vi.resetModules()
})

describe('useFanCardStore migration (AD-9)', () => {
  it('wraps a pre-9.3 single-card object (version 0) into a one-item collection', async () => {
    // Real pre-9.3 writes always include an explicit `version: 0` — zustand's
    // persist middleware serializes its own default version on every write,
    // even though the old store never set one explicitly.
    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        state: {
          driverId: 'norris',
          driverName: 'Lando Norris',
          constructorName: 'McLaren',
          circuitId: 'bahrain',
          circuitName: 'Bahrain International Circuit',
        },
        version: 0,
      }),
    )

    const { useFanCardStore } = await import('./useFanCardStore')
    await useFanCardStore.persist.rehydrate()
    const { cards } = useFanCardStore.getState()

    expect(cards).toHaveLength(1)
    expect(cards[0]).toMatchObject({
      driverId: 'norris',
      driverName: 'Lando Norris',
      constructorName: 'McLaren',
      circuitId: 'bahrain',
      circuitName: 'Bahrain International Circuit',
    })
    expect(cards[0].id).toBeTruthy()
  })

  it('migrates an empty pre-9.3 state (all-null picks) into zero cards', async () => {
    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        state: {
          driverId: null,
          driverName: null,
          constructorName: null,
          circuitId: null,
          circuitName: null,
        },
        version: 0,
      }),
    )

    const { useFanCardStore } = await import('./useFanCardStore')
    await useFanCardStore.persist.rehydrate()
    expect(useFanCardStore.getState().cards).toEqual([])
  })

  it('the storage key is unchanged across the migration', async () => {
    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        state: {
          driverId: 'norris',
          driverName: 'Lando Norris',
          constructorName: 'McLaren',
          circuitId: 'bahrain',
          circuitName: 'Bahrain International Circuit',
        },
        version: 0,
      }),
    )

    const { useFanCardStore } = await import('./useFanCardStore')
    await useFanCardStore.persist.rehydrate()

    expect(window.localStorage.getItem(STORAGE_KEY)).toBeTruthy()
    expect(window.localStorage.getItem('f1app__fanCard__v2')).toBeNull()
  })
})
