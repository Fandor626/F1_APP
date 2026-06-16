import { render, screen, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { CalendarPage } from './CalendarPage'
import { server } from '../../shared/test/server'
import type { RaceWeekend } from '../../shared/api/ergast'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CalendarPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

// Relative to the real current time (rather than a fixed mocked date) so this
// test doesn't need fake timers, which hang MSW's fetch interception. The
// exact days-until copy is covered separately by RaceWeekendCard.test.tsx.
const DAY_MS = 24 * 60 * 60 * 1000
const now = Date.now()

const pastRace: RaceWeekend = {
  season: 2026,
  round: 1,
  raceName: 'Past Race',
  circuitName: 'Past Circuit',
  locality: 'City A',
  country: 'Country A',
  weekendStart: new Date(now - 32 * DAY_MS).toISOString(),
  raceStart: new Date(now - 30 * DAY_MS).toISOString(),
}

const nextRace: RaceWeekend = {
  season: 2026,
  round: 2,
  raceName: 'Next Race',
  circuitName: 'Next Circuit',
  locality: 'City B',
  country: 'Country B',
  weekendStart: new Date(now + 1 * DAY_MS).toISOString(),
  raceStart: new Date(now + 3 * DAY_MS).toISOString(),
}

const futureRace: RaceWeekend = {
  season: 2026,
  round: 3,
  raceName: 'Future Race',
  circuitName: 'Future Circuit',
  locality: 'City C',
  country: 'Country C',
  weekendStart: new Date(now + 58 * DAY_MS).toISOString(),
  raceStart: new Date(now + 60 * DAY_MS).toISOString(),
}

describe('CalendarPage', () => {
  it('shows a skeleton while the schedule is loading', () => {
    renderPage()

    expect(screen.getByText('Race Calendar')).toBeInTheDocument()
    expect(screen.queryByText('Next race')).not.toBeInTheDocument()
  })

  it('pins the soonest upcoming race and lists every other race chronologically below it', async () => {
    server.use(
      http.get(`${API_BASE_URL}/api/races`, () =>
        HttpResponse.json([pastRace, nextRace, futureRace]),
      ),
    )

    renderPage()

    await waitFor(() => expect(screen.getByText(/Next race:/)).toBeInTheDocument())

    const nextSection = screen.getByRole('heading', { name: 'Next race' }).closest('section')!
    expect(within(nextSection).getByText('Next Race')).toBeInTheDocument()

    const restSection = screen.getByRole('heading', { name: 'Season schedule' }).closest('section')!
    const restCards = within(restSection).getAllByTestId('race-weekend-card')
    expect(restCards).toHaveLength(2)
    expect(within(restCards[0]).getByText('Past Race')).toBeInTheDocument()
    expect(within(restCards[1]).getByText('Future Race')).toBeInTheDocument()
  })

  it('shows a clear error message when the schedule request fails', async () => {
    server.use(http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.error()))

    renderPage()

    await waitFor(() =>
      expect(screen.getByText('Couldn\'t reach the server — try refreshing.')).toBeInTheDocument(),
    )
  })
})
