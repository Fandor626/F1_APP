import { create } from 'zustand'
import type { DriverState, LapTimeEntry } from '../../../shared/types/f1'

type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'

interface LiveRaceState {
  connectionStatus: ConnectionStatus
  drivers: Record<string, DriverState>
  lapChart: Record<string, LapTimeEntry[]>
  lastSnapshotTime: Date | null
  setConnectionStatus: (status: ConnectionStatus) => void
  setDrivers: (drivers: Record<string, DriverState>) => void
  setLapChart: (lapChart: Record<string, LapTimeEntry[]>) => void
  setLastSnapshotTime: (time: Date) => void
}

export const useLiveRaceStore = create<LiveRaceState>((set) => ({
  connectionStatus: 'disconnected',
  drivers: {},
  lapChart: {},
  lastSnapshotTime: null,
  setConnectionStatus: (status) => set({ connectionStatus: status }),
  setDrivers: (drivers) => set({ drivers }),
  setLapChart: (lapChart) => set({ lapChart }),
  setLastSnapshotTime: (time) => set({ lastSnapshotTime: time }),
}))
