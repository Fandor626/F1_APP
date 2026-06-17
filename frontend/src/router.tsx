import { createBrowserRouter } from 'react-router-dom'
import App from './App'
import { CalendarPage, RaceWeekendDetailView } from './features/calendar'
import { LiveRacePage } from './features/live-race'
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
    ],
  },
])
