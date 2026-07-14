import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { CircuitHistory } from './CircuitHistory'
import type { CircuitStats, CircuitWinner } from '../../shared/api/ergast'

const stats: CircuitStats = { lengthKm: 5.412, corners: 15, drsZones: 3 }
const winners: CircuitWinner[] = [
  { season: 2025, driverId: 'norris', driverName: 'Lando Norris', constructorName: 'McLaren' },
  { season: 2024, driverId: 'max_verstappen', driverName: 'Max Verstappen', constructorName: 'Red Bull Racing' },
]

function renderHistory(props: {
  firstF1Season?: number | null
  stats?: CircuitStats | null
  pastWinners?: CircuitWinner[]
}) {
  return render(
    <MemoryRouter>
      <CircuitHistory {...props} />
    </MemoryRouter>,
  )
}

describe('CircuitHistory', () => {
  it('renders four stat tiles and a past-winners list with driver-profile links', () => {
    renderHistory({ firstF1Season: 2004, stats, pastWinners: winners })

    expect(screen.getByText('5.412 km')).toBeInTheDocument()
    expect(screen.getByText('15')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
    expect(screen.getByText('2004')).toBeInTheDocument()

    expect(screen.getByRole('link', { name: 'Lando Norris' })).toHaveAttribute('href', '/drivers/norris')
    expect(screen.getByRole('link', { name: 'Max Verstappen' })).toHaveAttribute('href', '/drivers/max_verstappen')
  })

  it('shows stat tiles without a winners list when only partial data exists, rather than an error', () => {
    renderHistory({ firstF1Season: 2004, stats, pastWinners: [] })

    expect(screen.getByText('5.412 km')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('renders nothing when there is no historical data at all', () => {
    const { container } = renderHistory({ firstF1Season: null, stats: null, pastWinners: [] })

    expect(container.firstChild).toBeNull()
  })
})
