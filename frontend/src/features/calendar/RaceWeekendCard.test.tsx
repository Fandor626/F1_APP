import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
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
}

function renderCard(isNext = false) {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <RaceWeekendCard race={race} isNext={isNext} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

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
    renderCard(true)

    expect(screen.getByText(/^Next race: Italian Grand Prix, /)).toBeInTheDocument()
  })

  it('shows the top-3 driver and constructor standings once loaded', async () => {
    renderCard()

    await waitFor(() => expect(screen.getByText('Norris')).toBeInTheDocument())
    expect(screen.getByText("Drivers' Championship")).toBeInTheDocument()
    expect(screen.getByText("Constructors' Championship")).toBeInTheDocument()
    expect(screen.getByText('298 pts')).toBeInTheDocument()
    expect(screen.getByText('McLaren')).toBeInTheDocument()
  })
})
