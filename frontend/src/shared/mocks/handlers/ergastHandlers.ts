import { http, HttpResponse } from 'msw'
import type { RaceWeekend } from '../../api/ergast'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export const sampleRaceSchedule: RaceWeekend[] = [
  {
    season: 2026,
    round: 1,
    raceName: 'Bahrain Grand Prix',
    circuitName: 'Bahrain International Circuit',
    locality: 'Sakhir',
    country: 'Bahrain',
    raceStart: '2026-03-08T18:00:00+00:00',
  },
  {
    season: 2026,
    round: 2,
    raceName: 'Saudi Arabian Grand Prix',
    circuitName: 'Jeddah Corniche Circuit',
    locality: 'Jeddah',
    country: 'Saudi Arabia',
    raceStart: '2026-03-15T20:00:00+00:00',
  },
]

export const ergastHandlers = [
  http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.json(sampleRaceSchedule)),
]
