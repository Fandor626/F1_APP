import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { WinPredictionCallout } from './WinPredictionCallout'
import type { WinProbabilityEntry } from '../../shared/api/ergast'

// Deliberately NOT in probability-descending order — the backend sorts by
// grid position, and the callout must find the top pick by probability, not
// by array order.
const entries: WinProbabilityEntry[] = [
  { driverName: 'Max Verstappen', constructorName: 'Red Bull Racing', gridPosition: 1, winProbability: 38 },
  { driverName: 'Lando Norris', constructorName: 'McLaren', gridPosition: 2, winProbability: 27 },
  { driverName: 'Charles Leclerc', constructorName: 'Ferrari', gridPosition: 3, winProbability: 19 },
]

describe('WinPredictionCallout', () => {
  it('renders nothing when there are no entries', () => {
    const { container } = render(<WinPredictionCallout entries={[]} />)

    expect(container.firstChild).toBeNull()
  })

  it('names the highest-probability driver as the likely winner, not the first array entry', () => {
    const reordered = [entries[2], entries[0], entries[1]]
    render(<WinPredictionCallout entries={reordered} />)

    expect(screen.getByText('Max Verstappen')).toBeInTheDocument()
  })

  it('gives a pole-position reason when the top pick starts P1', () => {
    render(<WinPredictionCallout entries={entries} />)

    expect(screen.getByText(/starts from pole position/)).toBeInTheDocument()
  })

  it('shows no percentages by default', () => {
    render(<WinPredictionCallout entries={entries} />)

    expect(screen.queryByText(/%/)).not.toBeInTheDocument()
  })

  it('toggles the raw grid-by-grid table via aria-expanded/aria-controls', () => {
    render(<WinPredictionCallout entries={entries} />)

    const toggle = screen.getByRole('button', { name: /grid-by-grid win probability/ })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByText('38.0%')).not.toBeInTheDocument()

    fireEvent.click(toggle)

    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    const revealed = document.getElementById(toggle.getAttribute('aria-controls')!)
    expect(revealed).not.toBeNull()
    expect(screen.getByText('38.0%')).toBeInTheDocument()
  })
})
