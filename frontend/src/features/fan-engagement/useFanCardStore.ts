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

export interface FanCardEntry extends CompleteFanCardPicks {
  id: string
}

interface FanCardState {
  cards: FanCardEntry[]
  addCard: (picks: CompleteFanCardPicks) => void
}

export function hasFanCardPicks(picks: FanCardPicks): picks is CompleteFanCardPicks {
  return (
    picks.driverId !== null &&
    picks.driverName !== null &&
    picks.constructorName !== null &&
    picks.circuitId !== null &&
    picks.circuitName !== null
  )
}

export const useFanCardStore = create<FanCardState>()(
  persist(
    (set) => ({
      cards: [],
      addCard: (picks) => set((state) => ({ cards: [...state.cards, { ...picks, id: crypto.randomUUID() }] })),
    }),
    {
      name: 'f1app__fanCard__v1',
      version: 1,
      // AD-9: the pre-9.3 store persisted a single flat FanCardPicks object
      // (implicit version 0) under this same key. Wrap it into the new
      // { cards: [] } collection shape rather than renaming the key — an
      // existing user's card becomes their first card, never dropped.
      migrate: (persistedState, version) => {
        if (version === 0) {
          const old = persistedState as FanCardPicks
          return { cards: hasFanCardPicks(old) ? [{ ...old, id: crypto.randomUUID() }] : [] }
        }
        return persistedState as FanCardState
      },
    },
  ),
)
