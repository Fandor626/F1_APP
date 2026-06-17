export interface DriverState {
  driverNumber: number
  driverCode: string
  teamName: string
  teamColour: string
  position: number
  gapToCarAhead: string | null
  gapIsStale: boolean
  // Placeholders — null until Stories 2.2–2.4:
  tyreCompound: string | null
  stintLaps: number | null
  championshipDelta: string | null
}

export interface RaceStateSnapshot {
  capturedAt: string
  drivers: DriverState[]
}
