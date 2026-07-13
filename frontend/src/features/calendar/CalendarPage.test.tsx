import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { CalendarPage } from './CalendarPage'
import { server } from '../../shared/test/server'
import { sampleDriverStandings } from '../../shared/mocks/handlers/ergastHandlers'
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
  circuitId: 'past-circuit',
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
  circuitId: 'next-circuit',
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
  circuitId: 'future-circuit',
  circuitName: 'Future Circuit',
  locality: 'City C',
  country: 'Country C',
  weekendStart: new Date(now + 58 * DAY_MS).toISOString(),
  raceStart: new Date(now + 60 * DAY_MS).toISOString(),
}

function mockSchedule(races: RaceWeekend[]) {
  server.use(http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.json(races)))
}

describe('CalendarPage', () => {
  it('shows a skeleton while the schedule is loading', () => {
    renderPage()

    expect(screen.getByText('Race Calendar')).toBeInTheDocument()
    expect(screen.queryByText('Next race')).not.toBeInTheDocument()
  })

  it('defaults to the Future filter: pins the next race and hides past races', async () => {
    mockSchedule([pastRace, nextRace, futureRace])

    renderPage()

    await waitFor(() => expect(screen.getByText(/Next race:/)).toBeInTheDocument())

    expect(screen.getByRole('tab', { name: 'Future' })).toHaveAttribute('aria-selected', 'true')

    const nextSection = screen.getByRole('heading', { name: 'Next race' }).closest('section')!
    expect(within(nextSection).getByText('Next Race')).toBeInTheDocument()

    const restSection = screen.getByRole('heading', { name: 'Season schedule' }).closest('section')!
    const restCards = within(restSection).getAllByTestId('race-weekend-card')
    expect(restCards).toHaveLength(1)
    expect(within(restCards[0]).getByText('Future Race')).toBeInTheDocument()
    expect(screen.queryByText('Past Race')).not.toBeInTheDocument()
  })

  it('shows only completed races when the Past filter is selected, with no pinned next race', async () => {
    mockSchedule([pastRace, nextRace, futureRace])

    renderPage()
    await waitFor(() => expect(screen.getByText(/Next race:/)).toBeInTheDocument())

    fireEvent.click(screen.getByRole('tab', { name: 'Past' }))

    expect(screen.queryByRole('heading', { name: 'Next race' })).not.toBeInTheDocument()
    const restSection = screen.getByRole('heading', { name: 'Season schedule' }).closest('section')!
    const restCards = within(restSection).getAllByTestId('race-weekend-card')
    expect(restCards).toHaveLength(1)
    expect(within(restCards[0]).getByText('Past Race')).toBeInTheDocument()
  })

  it('shows the full unfiltered season when the All filter is selected', async () => {
    mockSchedule([pastRace, nextRace, futureRace])

    renderPage()
    await waitFor(() => expect(screen.getByText(/Next race:/)).toBeInTheDocument())

    fireEvent.click(screen.getByRole('tab', { name: 'All' }))

    const nextSection = screen.getByRole('heading', { name: 'Next race' }).closest('section')!
    expect(within(nextSection).getByText('Next Race')).toBeInTheDocument()

    const restSection = screen.getByRole('heading', { name: 'Season schedule' }).closest('section')!
    const restCards = within(restSection).getAllByTestId('race-weekend-card')
    expect(restCards).toHaveLength(2)
    expect(within(restCards[0]).getByText('Past Race')).toBeInTheDocument()
    expect(within(restCards[1]).getByText('Future Race')).toBeInTheDocument()
  })

  it('moves selection between filter tabs with arrow keys, wrapping at the ends', async () => {
    mockSchedule([pastRace, nextRace, futureRace])

    renderPage()
    await waitFor(() => expect(screen.getByText(/Next race:/)).toBeInTheDocument())

    const futureTab = screen.getByRole('tab', { name: 'Future' })
    futureTab.focus()

    fireEvent.keyDown(futureTab, { key: 'ArrowRight' })
    const pastTab = screen.getByRole('tab', { name: 'Past' })
    expect(pastTab).toHaveAttribute('aria-selected', 'true')
    expect(pastTab).toHaveFocus()

    fireEvent.keyDown(pastTab, { key: 'ArrowRight' })
    const allTab = screen.getByRole('tab', { name: 'All' })
    expect(allTab).toHaveAttribute('aria-selected', 'true')
    expect(allTab).toHaveFocus()
  })

  it('keeps the Championship Sidebar visible and unreset across every filter tab', async () => {
    mockSchedule([pastRace, nextRace, futureRace])

    renderPage()
    await waitFor(() => expect(screen.getByText(/Next race:/)).toBeInTheDocument())
    await waitFor(() =>
      expect(screen.getByRole('complementary', { name: 'Championship standings' })).toBeInTheDocument(),
    )
    // Scoped to the sidebar landmark: RaceWeekendCard also renders driver
    // names in its own standings preview until Story 7.4 removes it, so an
    // unscoped query would match more than one element on this page today.
    const sidebar = () => screen.getByRole('complementary', { name: 'Championship standings' })
    expect(within(sidebar()).getByText(sampleDriverStandings[0].driverName)).toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: 'Past' }))
    expect(within(sidebar()).getByText(sampleDriverStandings[0].driverName)).toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: 'All' }))
    expect(within(sidebar()).getByText(sampleDriverStandings[0].driverName)).toBeInTheDocument()
  })

  it('shows a clear error message when the schedule request fails', async () => {
    server.use(http.get(`${API_BASE_URL}/api/races`, () => HttpResponse.error()))

    renderPage()

    await waitFor(() =>
      expect(screen.getByText('Couldn\'t reach the server — try refreshing.')).toBeInTheDocument(),
    )
  })
})
