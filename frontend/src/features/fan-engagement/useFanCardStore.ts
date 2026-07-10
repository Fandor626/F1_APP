import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface FanCardPicks {
  driverId: string | null
  driverName: string | null
  constructorName: string | null
  circuitId: string | null
  circuitName: string | null
}

export interface CompleteFanCardPicks {
  driverId: string
  driverName: string
  constructorName: string
  circuitId: string
  circuitName: string
}

interface FanCardState extends FanCardPicks {
  setDriverPick: (driverId: string, driverName: string) => void
  setConstructorPick: (constructorName: string) => void
  setCircuitPick: (circuitId: string, circuitName: string) => void
  resetFanCard: () => void
}

const EMPTY_PICKS: FanCardPicks = {
  driverId: null,
  driverName: null,
  constructorName: null,
  circuitId: null,
  circuitName: null,
}

export const useFanCardStore = create<FanCardState>()(
  persist(
    (set) => ({
      ...EMPTY_PICKS,
      setDriverPick: (driverId, driverName) => set({ driverId, driverName }),
      setConstructorPick: (constructorName) => set({ constructorName }),
      setCircuitPick: (circuitId, circuitName) => set({ circuitId, circuitName }),
      resetFanCard: () => set(EMPTY_PICKS),
    }),
    { name: 'f1app__fanCard__v1' },
  ),
)

export function hasFanCardPicks(picks: FanCardPicks): picks is CompleteFanCardPicks {
  return (
    picks.driverId !== null &&
    picks.driverName !== null &&
    picks.constructorName !== null &&
    picks.circuitId !== null &&
    picks.circuitName !== null
  )
}
