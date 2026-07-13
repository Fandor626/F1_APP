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
  x: number | null
  y: number | null
  miniSectorStatus: 'purple' | 'green' | 'yellow' | 'white' | null
  pitWindowActive: boolean
}

export interface FastestSectorEntry {
  driverNumber: number
  driverCode: string
  teamColour: string
  timeSeconds: number
}

export interface FastestSectorBoard {
  s1: FastestSectorEntry | null
  s2: FastestSectorEntry | null
  s3: FastestSectorEntry | null
}

export interface RaceTimelineEvent {
  lapNumber: number
  eventType: 'SafetyCar' | 'VirtualSafetyCar' | 'RedFlag' | 'PitStop' | 'Dnf' | 'FastestLap'
  driverCode: string | null
  detail: string | null
}

export interface RaceStateSnapshot {
  capturedAt: string
  drivers: DriverState[]
  // Key is driverNumber as string (JSON object keys are always strings)
  lapChart: Record<string, LapTimeEntry[]>
  sessionMode: 'live' | 'stale' | 'fallback'
  fallbackRaceName: string | null
  circuitId: string | null
  fastestSectors: FastestSectorBoard | null
  timeline: RaceTimelineEvent[]
}

export interface LastRaceResult {
  raceName: string
  raceDate: string
  drivers: DriverState[]
  season: number
  round: number
}
