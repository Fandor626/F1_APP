import { create } from 'zustand'
import type { DriverState } from '../../../shared/types/f1'

type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected'

interface LiveRaceState {
  connectionStatus: ConnectionStatus
  drivers: Record<string, DriverState>
  lastSnapshotTime: Date | null
  setConnectionStatus: (status: ConnectionStatus) => void
  setDrivers: (drivers: Record<string, DriverState>) => void
  setLastSnapshotTime: (time: Date) => void
}

export const useLiveRaceStore = create<LiveRaceState>((set) => ({
  connectionStatus: 'disconnected',
  drivers: {},
  lastSnapshotTime: null,
  setConnectionStatus: (status) => set({ connectionStatus: status }),
  setDrivers: (drivers) => set({ drivers }),
  setLastSnapshotTime: (time) => set({ lastSnapshotTime: time }),
}))
