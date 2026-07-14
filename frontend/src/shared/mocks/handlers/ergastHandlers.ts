import { http, HttpResponse } from 'msw'
import type {
  CircuitProfile,
  ConstructorStanding,
  DriverOption,
  DriverProfile,
  DriverStanding,
  DriverTrajectory,
  HeadToHeadComparison,
  RaceWeekend,
  RaceWeekendDetail,
  SeasonWrapped,
} from '../../api/ergast'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export const sampleRaceSchedule: RaceWeekend[] = [
  {
    season: 2026,
    round: 1,
    raceName: 'Bahrain Grand Prix',
    circuitId: 'bahrain',
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
    circuitId: 'jeddah',
    circuitName: 'Jeddah Corniche Circuit',
    locality: 'Jeddah',
    country: 'Saudi Arabia',
    weekendStart: '2026-03-13T13:30:00+00:00',
    raceStart: '2026-03-15T20:00:00+00:00',
  },
]

export const sampleDriverStandings: DriverStanding[] = [
  { position: 1, driverId: 'norris', driverName: 'Norris', constructorName: 'McLaren', points: 298, wins: 7, nationality: 'British' },
  { position: 2, driverId: 'max_verstappen', driverName: 'Verstappen', constructorName: 'Red Bull Racing', points: 277, wins: 5, nationality: 'Dutch' },
  { position: 3, driverId: 'leclerc', driverName: 'Leclerc', constructorName: 'Ferrari', points: 229, wins: 2, nationality: 'Monegasque' },
  { position: 4, driverId: 'piastri', driverName: 'Piastri', constructorName: 'McLaren', points: 214, wins: 3, nationality: 'Australian' },
  { position: 5, driverId: 'russell', driverName: 'Russell', constructorName: 'Mercedes', points: 187, wins: 1, nationality: 'British' },
  { position: 6, driverId: 'hamilton', driverName: 'Hamilton', constructorName: 'Ferrari', points: 163, wins: 0, nationality: 'British' },
  { position: 7, driverId: 'antonelli', driverName: 'Antonelli', constructorName: 'Mercedes', points: 121, wins: 0, nationality: 'Italian' },
  { position: 8, driverId: 'perez', driverName: 'Pérez', constructorName: 'Red Bull Racing', points: 98, wins: 0, nationality: 'Mexican' },
]

export const sampleConstructorStandings: ConstructorStanding[] = [
  { position: 1, constructorName: 'McLaren', points: 512, wins: 10, nationality: 'British' },
  { position: 2, constructorName: 'Ferrari', points: 392, wins: 2, nationality: 'Italian' },
  { position: 3, constructorName: 'Red Bull Racing', points: 375, wins: 5, nationality: 'Austrian' },
  { position: 4, constructorName: 'Mercedes', points: 308, wins: 1, nationality: 'German' },
]

export const sampleSeasonWrapped: SeasonWrapped = {
  mostDramaticRace: { raceName: 'Brazilian Grand Prix', round: 21, totalPositionSwing: 64 },
  mostDnfs: { driverId: 'perez', driverName: 'Pérez', constructorName: 'Red Bull Racing', value: 5 },
  biggestPointsComeback: { driverId: 'russell', driverName: 'Russell', constructorName: 'Mercedes', value: 87 },
  mostPositionsGainedInARace: {
    driverId: 'hamilton',
    driverName: 'Hamilton',
    constructorName: 'Ferrari',
    raceName: 'Dutch Grand Prix',
    positionsGained: 15,
  },
  mostImprovedConstructor: {
    constructorName: 'Mercedes',
    earlySeasonPosition: 5,
    finalPosition: 2,
    positionsImproved: 3,
  },
}

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
    circuitId: 'bahrain',
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
    allTimeLapRecord: { driverId: 'hamilton', driverName: 'Lewis Hamilton', constructorName: 'Mercedes', time: '1:31.447', season: 2019 },
    recentLapRecord: { driverId: 'norris', driverName: 'Lando Norris', constructorName: 'McLaren', time: '1:32.608', season: 2026 },
  },
  2: {
    season: 2026,
    round: 2,
    raceName: 'Saudi Arabian Grand Prix',
    circuitId: 'jeddah',
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

export const sampleCircuitProfile: CircuitProfile = {
  circuitId: 'monza',
  circuitName: 'Autodromo Nazionale di Monza',
  locality: 'Monza',
  country: 'Italy',
  firstF1Season: 1950,
  lapRecord: { driverId: 'hamilton', driverName: 'Lewis Hamilton', constructorName: 'Mercedes', time: '1:21.046', season: 2020 },
  pastWinners: [
    { season: 2025, driverName: 'Lando Norris', constructorName: 'McLaren' },
    { season: 2024, driverName: 'Charles Leclerc', constructorName: 'Ferrari' },
  ],
  stats: { lengthKm: 5.793, corners: 11, drsZones: 2 },
}

export const sampleDriverProfile: DriverProfile = {
  driverId: 'max_verstappen',
  fullName: 'Max Verstappen',
  nationality: 'Dutch',
  careerTotals: { races: 218, wins: 65, podiums: 116, poles: 44, fastestLaps: 33, titles: 4 },
  constructorHistory: [
    { season: 2015, constructorNames: ['Toro Rosso'] },
    { season: 2016, constructorNames: ['Toro Rosso', 'Red Bull Racing'] },
    { season: 2024, constructorNames: ['Red Bull Racing'] },
  ],
  careerPoints: [
    { season: 2024, round: 1, raceName: 'Bahrain Grand Prix', pointsThisRound: 26, cumulativePoints: 26 },
    { season: 2024, round: 2, raceName: 'Saudi Arabian Grand Prix', pointsThisRound: 25, cumulativePoints: 51 },
  ],
}

export const sampleDriverOptions: DriverOption[] = [
  { driverId: 'max_verstappen', fullName: 'Max Verstappen' },
  { driverId: 'hamilton', fullName: 'Lewis Hamilton' },
  { driverId: 'norris', fullName: 'Lando Norris' },
]

export const sampleHeadToHeadComparison: HeadToHeadComparison = {
  driverA: {
    driverId: 'max_verstappen',
    fullName: 'Max Verstappen',
    qualifyingAveragePosition: 2.4,
    raceFinishAveragePosition: 2.1,
    dnfCount: 12,
    pointsScored: 2586,
    fastestLaps: 33,
    wins: 65,
    racesCompared: 218,
  },
  driverB: {
    driverId: 'hamilton',
    fullName: 'Lewis Hamilton',
    qualifyingAveragePosition: 3.1,
    raceFinishAveragePosition: 3.4,
    dnfCount: 24,
    pointsScored: 4862,
    fastestLaps: 61,
    wins: 105,
    racesCompared: 350,
  },
}

export const ergastHandlers = [
  http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.json(sampleRaceSchedule)),
  http.get(`${API_BASE_URL}/api/standings/drivers`, () => HttpResponse.json(sampleDriverStandings)),
  http.get(`${API_BASE_URL}/api/standings/constructors`, () => HttpResponse.json(sampleConstructorStandings)),
  http.get(`${API_BASE_URL}/api/standings/trajectory`, () => HttpResponse.json(sampleTrajectory)),
  http.get(`${API_BASE_URL}/api/standings/season-wrapped`, () => HttpResponse.json(sampleSeasonWrapped)),
  http.get(`${API_BASE_URL}/api/races/:round/win-probability`, () => HttpResponse.json([])),
  http.get(`${API_BASE_URL}/api/races/:round`, ({ params }) => {
    const detail = sampleRaceDetailsByRound[Number(params.round)]
    return detail ? HttpResponse.json(detail) : new HttpResponse(null, { status: 404 })
  }),
  http.get(`${API_BASE_URL}/api/circuits/:circuitId`, ({ params }) => {
    return params.circuitId === sampleCircuitProfile.circuitId
      ? HttpResponse.json(sampleCircuitProfile)
      : new HttpResponse(null, { status: 404 })
  }),
  // Registered before /api/drivers/:driverId so these more specific paths
  // win MSW's first-match handler order.
  http.get(`${API_BASE_URL}/api/drivers/compare`, ({ request }) => {
    const url = new URL(request.url)
    const driverA = url.searchParams.get('driverA')
    const driverB = url.searchParams.get('driverB')
    const known = new Set([sampleHeadToHeadComparison.driverA!.driverId, sampleHeadToHeadComparison.driverB!.driverId])
    return driverA && driverB && known.has(driverA) && known.has(driverB)
      ? HttpResponse.json(sampleHeadToHeadComparison)
      : new HttpResponse(null, { status: 404 })
  }),
  http.get(`${API_BASE_URL}/api/drivers`, () => HttpResponse.json(sampleDriverOptions)),
  http.get(`${API_BASE_URL}/api/drivers/:driverId`, ({ params }) => {
    return params.driverId === sampleDriverProfile.driverId
      ? HttpResponse.json(sampleDriverProfile)
      : new HttpResponse(null, { status: 404 })
  }),
]
