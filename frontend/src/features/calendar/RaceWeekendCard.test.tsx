import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { RaceWeekendCard } from './RaceWeekendCard'
import type { RaceWeekend } from '../../shared/api/ergast'

const race: RaceWeekend = {
  season: 2026,
  round: 14,
  raceName: 'Italian Grand Prix',
  circuitName: 'Autodromo Nazionale di Monza',
  locality: 'Monza',
  country: 'Italy',
  raceStart: '2026-09-07T13:00:00+00:00',
}

afterEach(() => {
  vi.useRealTimers()
})

describe('RaceWeekendCard', () => {
  it('renders the race name and formatted start time', () => {
    render(<RaceWeekendCard race={race} />)

    expect(screen.getByRole('heading', { name: 'Italian Grand Prix' })).toBeInTheDocument()
  })

  it('does not show the next-race badge by default', () => {
    render(<RaceWeekendCard race={race} />)

    expect(screen.queryByText(/Next race:/)).not.toBeInTheDocument()
  })

  it('shows a next-race badge with a days-until countdown when pinned', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-09-04T00:00:00Z'))

    render(<RaceWeekendCard race={race} isNext />)

    expect(screen.getByText('Next race: Italian Grand Prix, 3 days')).toBeInTheDocument()
  })
})
