import { create } from 'zustand'
import type { DriverState, LapTimeEntry } from '../../../shared/types/f1'

type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'
type SessionMode = 'live' | 'stale' | 'fallback'

interface LiveRaceState {
  connectionStatus: ConnectionStatus
  sessionMode: SessionMode
  fallbackRaceName: string | null
  circuitId: string | null
  drivers: Record<string, DriverState>
  lapChart: Record<string, LapTimeEntry[]>
  lastSnapshotTime: Date | null
  setConnectionStatus: (status: ConnectionStatus) => void
  setSessionMode: (mode: SessionMode) => void
  setFallbackRaceName: (name: string | null) => void
  setCircuitId: (id: string | null) => void
  setDrivers: (drivers: Record<string, DriverState>) => void
  setLapChart: (lapChart: Record<string, LapTimeEntry[]>) => void
  setLastSnapshotTime: (time: Date) => void
}

export const useLiveRaceStore = create<LiveRaceState>((set) => ({
  connectionStatus: 'disconnected',
  sessionMode: 'live',
  fallbackRaceName: null,
  circuitId: null,
  drivers: {},
  lapChart: {},
  lastSnapshotTime: null,
  setConnectionStatus: (status) => set({ connectionStatus: status }),
  setSessionMode: (mode) => set({ sessionMode: mode }),
  setFallbackRaceName: (name) => set({ fallbackRaceName: name }),
  setCircuitId: (id) => set({ circuitId: id }),
  setDrivers: (drivers) => set({ drivers }),
  setLapChart: (lapChart) => set({ lapChart }),
  setLastSnapshotTime: (time) => set({ lastSnapshotTime: time }),
}))
