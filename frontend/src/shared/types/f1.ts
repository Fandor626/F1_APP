export interface LapTimeEntry {
  lapNumber: number
  lapDurationSeconds: number | null
  isPitOutLap: boolean
}

export interface DriverState {
  driverNumber: number
  driverCode: string
  teamName: string
  teamColour: string
  position: number
  gapToCarAhead: string | null
  gapIsStale: boolean
  tyreCompound: string | null
  stintLaps: number | null
  championshipDelta: string | null
}

export interface RaceStateSnapshot {
  capturedAt: string
  drivers: DriverState[]
  // Key is driverNumber as string (JSON object keys are always strings)
  lapChart: Record<string, LapTimeEntry[]>
}
