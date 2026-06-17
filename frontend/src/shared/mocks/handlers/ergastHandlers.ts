import { http, HttpResponse } from 'msw'
import type { ConstructorStanding, DriverStanding, RaceWeekend, RaceWeekendDetail } from '../../api/ergast'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export const sampleRaceSchedule: RaceWeekend[] = [
  {
    season: 2026,
    round: 1,
    raceName: 'Bahrain Grand Prix',
    circuitName: 'Bahrain International Circuit',
    locality: 'Sakhir',
    country: 'Bahrain',
    weekendStart: '2026-03-06T13:30:00+00:00',
    raceStart: '2026-03-08T18:00:00+00:00',
  },
  {
    season: 2026,
    round: 2,
    raceName: 'Saudi Arabian Grand Prix',
    circuitName: 'Jeddah Corniche Circuit',
    locality: 'Jeddah',
    country: 'Saudi Arabia',
    weekendStart: '2026-03-13T13:30:00+00:00',
    raceStart: '2026-03-15T20:00:00+00:00',
  },
]

export const sampleDriverStandings: DriverStanding[] = [
  { position: 1, driverName: 'Norris', constructorName: 'McLaren', points: 312 },
  { position: 2, driverName: 'Verstappen', constructorName: 'Red Bull Racing', points: 289 },
  { position: 3, driverName: 'Leclerc', constructorName: 'Ferrari', points: 241 },
]

export const sampleConstructorStandings: ConstructorStanding[] = [
  { position: 1, constructorName: 'McLaren', points: 567 },
  { position: 2, constructorName: 'Ferrari', points: 438 },
  { position: 3, constructorName: 'Red Bull Racing', points: 401 },
]

export const sampleRaceDetailsByRound: Record<number, RaceWeekendDetail> = {
  1: {
    season: 2026,
    round: 1,
    raceName: 'Bahrain Grand Prix',
    circuitName: 'Bahrain International Circuit',
    country: 'Bahrain',
    sessions: [
      { name: 'FP1', start: '2026-03-06T13:30:00+00:00' },
      { name: 'FP2', start: '2026-03-06T17:00:00+00:00' },
      { name: 'FP3', start: '2026-03-07T13:30:00+00:00' },
      { name: 'Qualifying', start: '2026-03-07T17:00:00+00:00' },
      { name: 'Race', start: '2026-03-08T18:00:00+00:00' },
    ],
    priorYearWinner: { driverName: 'Oscar Piastri', constructorName: 'McLaren', time: '1:35:39.435' },
    championshipDelta: { leaderName: 'Lando Norris', runnerUpName: 'Max Verstappen', pointsGap: 23 },
  },
  2: {
    season: 2026,
    round: 2,
    raceName: 'Saudi Arabian Grand Prix',
    circuitName: 'Jeddah Corniche Circuit',
    country: 'Saudi Arabia',
    sessions: [
      { name: 'FP1', start: '2026-03-13T15:30:00+00:00' },
      { name: 'Sprint Qualifying', start: '2026-03-13T19:30:00+00:00' },
      { name: 'Sprint', start: '2026-03-14T15:00:00+00:00' },
      { name: 'Qualifying', start: '2026-03-14T19:00:00+00:00' },
      { name: 'Race', start: '2026-03-15T20:00:00+00:00' },
    ],
    championshipDelta: { leaderName: 'Lando Norris', runnerUpName: 'Max Verstappen', pointsGap: 23 },
  },
}

export const ergastHandlers = [
  http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.json(sampleRaceSchedule)),
  http.get(`${API_BASE_URL}/api/standings/drivers`, () => HttpResponse.json(sampleDriverStandings)),
  http.get(`${API_BASE_URL}/api/standings/constructors`, () => HttpResponse.json(sampleConstructorStandings)),
  http.get(`${API_BASE_URL}/api/races/:round`, ({ params }) => {
    const detail = sampleRaceDetailsByRound[Number(params.round)]
    return detail ? HttpResponse.json(detail) : new HttpResponse(null, { status: 404 })
  }),
]
