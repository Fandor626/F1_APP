import { http, HttpResponse } from 'msw'
import type { ConstructorStanding, DriverStanding, DriverTrajectory, RaceWeekend, RaceWeekendDetail } from '../../api/ergast'

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
  { position: 1, driverName: 'Norris', constructorName: 'McLaren', points: 298, wins: 7, nationality: 'British' },
  { position: 2, driverName: 'Verstappen', constructorName: 'Red Bull Racing', points: 277, wins: 5, nationality: 'Dutch' },
  { position: 3, driverName: 'Leclerc', constructorName: 'Ferrari', points: 229, wins: 2, nationality: 'Monegasque' },
  { position: 4, driverName: 'Piastri', constructorName: 'McLaren', points: 214, wins: 3, nationality: 'Australian' },
  { position: 5, driverName: 'Russell', constructorName: 'Mercedes', points: 187, wins: 1, nationality: 'British' },
  { position: 6, driverName: 'Hamilton', constructorName: 'Ferrari', points: 163, wins: 0, nationality: 'British' },
  { position: 7, driverName: 'Antonelli', constructorName: 'Mercedes', points: 121, wins: 0, nationality: 'Italian' },
  { position: 8, driverName: 'Pérez', constructorName: 'Red Bull Racing', points: 98, wins: 0, nationality: 'Mexican' },
]

export const sampleConstructorStandings: ConstructorStanding[] = [
  { position: 1, constructorName: 'McLaren', points: 512, wins: 10, nationality: 'British' },
  { position: 2, constructorName: 'Ferrari', points: 392, wins: 2, nationality: 'Italian' },
  { position: 3, constructorName: 'Red Bull Racing', points: 375, wins: 5, nationality: 'Austrian' },
  { position: 4, constructorName: 'Mercedes', points: 308, wins: 1, nationality: 'German' },
]

export const sampleTrajectory: DriverTrajectory[] = [
  {
    driverId: 'norris',
    driverName: 'Norris',
    constructorName: 'McLaren',
    points: [
      { round: 1, raceName: 'Bahrain Grand Prix', resultPosition: 1, pointsThisRound: 25, cumulativePoints: 25 },
      { round: 2, raceName: 'Saudi Arabian Grand Prix', resultPosition: 2, pointsThisRound: 18, cumulativePoints: 43 },
      { round: 3, raceName: 'Australian Grand Prix', resultPosition: 1, pointsThisRound: 25, cumulativePoints: 68 },
    ],
  },
  {
    driverId: 'verstappen',
    driverName: 'Verstappen',
    constructorName: 'Red Bull Racing',
    points: [
      { round: 1, raceName: 'Bahrain Grand Prix', resultPosition: 2, pointsThisRound: 18, cumulativePoints: 18 },
      { round: 2, raceName: 'Saudi Arabian Grand Prix', resultPosition: 1, pointsThisRound: 25, cumulativePoints: 43 },
      { round: 3, raceName: 'Australian Grand Prix', resultPosition: 3, pointsThisRound: 15, cumulativePoints: 58 },
    ],
  },
  {
    driverId: 'leclerc',
    driverName: 'Leclerc',
    constructorName: 'Ferrari',
    points: [
      { round: 1, raceName: 'Bahrain Grand Prix', resultPosition: 3, pointsThisRound: 15, cumulativePoints: 15 },
      { round: 2, raceName: 'Saudi Arabian Grand Prix', resultPosition: 4, pointsThisRound: 12, cumulativePoints: 27 },
      { round: 3, raceName: 'Australian Grand Prix', resultPosition: 2, pointsThisRound: 18, cumulativePoints: 45 },
    ],
  },
  {
    driverId: 'russell',
    driverName: 'Russell',
    constructorName: 'Mercedes',
    points: [
      { round: 1, raceName: 'Bahrain Grand Prix', resultPosition: 5, pointsThisRound: 10, cumulativePoints: 10 },
      { round: 2, raceName: 'Saudi Arabian Grand Prix', resultPosition: 3, pointsThisRound: 15, cumulativePoints: 25 },
      { round: 3, raceName: 'Australian Grand Prix', resultPosition: 4, pointsThisRound: 12, cumulativePoints: 37 },
    ],
  },
]

export const sampleRaceDetailsByRound: Record<number, RaceWeekendDetail> = {
  1: {
    season: 2026,
    round: 1,
    raceName: 'Bahrain Grand Prix',
    circuitName: 'Bahrain International Circuit',
    country: 'Bahrain',
    sessions: [
      { name: 'FP1', start: '2026-03-06T16:30:00+03:00' },
      { name: 'FP2', start: '2026-03-06T20:00:00+03:00' },
      { name: 'FP3', start: '2026-03-07T16:30:00+03:00' },
      { name: 'Qualifying', start: '2026-03-07T20:00:00+03:00' },
      { name: 'Race', start: '2026-03-08T21:00:00+03:00' },
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
      { name: 'FP1', start: '2026-03-13T18:30:00+03:00' },
      { name: 'Sprint Qualifying', start: '2026-03-13T22:30:00+03:00' },
      { name: 'Sprint', start: '2026-03-14T18:00:00+03:00' },
      { name: 'Qualifying', start: '2026-03-14T22:00:00+03:00' },
      { name: 'Race', start: '2026-03-15T23:00:00+03:00' },
    ],
    championshipDelta: { leaderName: 'Lando Norris', runnerUpName: 'Max Verstappen', pointsGap: 23 },
  },
}

export const ergastHandlers = [
  http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.json(sampleRaceSchedule)),
  http.get(`${API_BASE_URL}/api/standings/drivers`, () => HttpResponse.json(sampleDriverStandings)),
  http.get(`${API_BASE_URL}/api/standings/constructors`, () => HttpResponse.json(sampleConstructorStandings)),
  http.get(`${API_BASE_URL}/api/standings/trajectory`, () => HttpResponse.json(sampleTrajectory)),
  http.get(`${API_BASE_URL}/api/races/:round/win-probability`, () => HttpResponse.json([])),
  http.get(`${API_BASE_URL}/api/races/:round`, ({ params }) => {
    const detail = sampleRaceDetailsByRound[Number(params.round)]
    return detail ? HttpResponse.json(detail) : new HttpResponse(null, { status: 404 })
  }),
]
