import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RaceWeekendCard } from './RaceWeekendCard'
import type { RaceWeekend } from '../../shared/api/ergast'

const race: RaceWeekend = {
  season: 2026,
  round: 14,
  raceName: 'Italian Grand Prix',
  circuitId: 'monza',
  circuitName: 'Autodromo Nazionale di Monza',
  locality: 'Monza',
  country: 'Italy',
  weekendStart: '2026-09-05T10:30:00+00:00',
  raceStart: '2026-09-07T13:00:00+00:00',
  allTimeLapRecord: { driverId: 'barrichello', driverName: 'Rubens Barrichello', constructorName: 'Ferrari', time: '1:21.046', season: 2004 },
  recentLapRecord: { driverId: 'norris', driverName: 'Lando Norris', constructorName: 'McLaren', time: '1:22.708', season: 2025 },
}

const raceWithoutLapData: RaceWeekend = {
  ...race,
  allTimeLapRecord: null,
  recentLapRecord: null,
}

function renderCard(raceWeekend: RaceWeekend = race, isNext = false) {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <RaceWeekendCard race={raceWeekend} isNext={isNext} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.resetAllMocks()
  // TrackOutline's fetch is unrelated to what most of these tests assert on —
  // default it to a clean failure so it degrades gracefully and doesn't leave
  // an unresolved promise dangling across tests.
  globalThis.fetch = vi.fn().mockResolvedValue({ ok: false } as Response)
})

describe('RaceWeekendCard', () => {
  it('renders the race name, circuit name, and country flag', () => {
    renderCard()

    expect(screen.getByRole('heading', { name: 'Italian Grand Prix' })).toBeInTheDocument()
    expect(screen.getByText('Autodromo Nazionale di Monza')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Italy flag' })).toBeInTheDocument()
  })

  it('links the whole card to its detail view', () => {
    renderCard()

    expect(screen.getByTestId('race-weekend-card')).toHaveAttribute('href', '/races/14')
  })

  it('does not show the next-race badge by default', () => {
    renderCard()

    expect(screen.queryByText(/Next race:/)).not.toBeInTheDocument()
  })

  it('shows a next-race badge naming the race when pinned', () => {
    renderCard(race, true)

    expect(screen.getByText(/^Next race: Italian Grand Prix, /)).toBeInTheDocument()
  })

  it('no longer shows driver or constructor standings', () => {
    renderCard()

    expect(screen.queryByText("Drivers' Championship")).not.toBeInTheDocument()
    expect(screen.queryByText("Constructors' Championship")).not.toBeInTheDocument()
  })

  it('shows the all-time and recent-year fastest lap, each with driver name', () => {
    renderCard()

    expect(screen.getByText('All-time fastest lap')).toBeInTheDocument()
    expect(screen.getByText(/1:21\.046/)).toBeInTheDocument()
    expect(screen.getByText('Rubens Barrichello')).toBeInTheDocument()

    expect(screen.getByText('2025 fastest lap')).toBeInTheDocument()
    expect(screen.getByText(/1:22\.708/)).toBeInTheDocument()
    expect(screen.getByText('Lando Norris')).toBeInTheDocument()
  })

  it('omits the fastest-lap block gracefully when no lap data is available', () => {
    renderCard(raceWithoutLapData)

    expect(screen.queryByText('All-time fastest lap')).not.toBeInTheDocument()
    expect(screen.queryByText(/fastest lap/)).not.toBeInTheDocument()
  })

  it('renders the accessible track outline when the circuit-configs fetch succeeds', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ circuitId: 'monza', viewBox: '0 0 500 500', trackPath: 'M10,10 L90,90 Z' }),
    } as Response)

    renderCard()

    await waitFor(() =>
      expect(
        screen.getByRole('img', { name: 'Track layout: Autodromo Nazionale di Monza' }),
      ).toBeInTheDocument(),
    )
  })
})
