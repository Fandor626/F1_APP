import { createBrowserRouter } from 'react-router-dom'
import App from './App'
import { CalendarPage, RaceWeekendDetailView } from './features/calendar'
import { LiveRacePage } from './features/live-race'
import { CircuitProfilePage, DriverProfilePage, HeadToHeadPage } from './features/profiles'
import { StandingsPage } from './features/standings'
import { ErrorBoundary } from './shared/components/ErrorBoundary'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    errorElement: <ErrorBoundary />,
    children: [
      { index: true, element: <CalendarPage /> },
      { path: 'races/:round', element: <RaceWeekendDetailView /> },
      { path: 'live', element: <LiveRacePage /> },
      { path: 'standings', element: <StandingsPage /> },
      { path: 'circuits/:circuitId', element: <CircuitProfilePage /> },
      { path: 'drivers/:driverId', element: <DriverProfilePage /> },
      { path: 'head-to-head', element: <HeadToHeadPage /> },
    ],
  },
])
