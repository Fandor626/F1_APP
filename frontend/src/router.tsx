import { createBrowserRouter } from 'react-router-dom'
import App from './App'
import { CalendarPage, RaceWeekendDetailView } from './features/calendar'
import { ErrorBoundary } from './shared/components/ErrorBoundary'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    errorElement: <ErrorBoundary />,
    children: [
      { index: true, element: <CalendarPage /> },
      { path: 'races/:round', element: <RaceWeekendDetailView /> },
    ],
  },
])
