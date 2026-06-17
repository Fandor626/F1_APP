import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { WinProbabilityWidget } from './WinProbabilityWidget'
import type { WinProbabilityEntry } from '../../shared/api/ergast'

const sampleEntries: WinProbabilityEntry[] = [
  { gridPosition: 1, driverName: 'Lando Norris', constructorName: 'McLaren', winProbability: 52.3 },
  { gridPosition: 2, driverName: 'Oscar Piastri', constructorName: 'McLaren', winProbability: 28.7 },
  { gridPosition: 3, driverName: 'Max Verstappen', constructorName: 'Red Bull', winProbability: 19.0 },
]

describe('WinProbabilityWidget', () => {
  it('renders nothing when entries list is empty', () => {
    const { container } = render(<WinProbabilityWidget entries={[]} />)

    expect(container).toBeEmptyDOMElement()
  })

  it('renders each driver name, constructor, and grid position', () => {
    render(<WinProbabilityWidget entries={sampleEntries} />)

    expect(screen.getByText('Lando Norris')).toBeInTheDocument()
    expect(screen.getAllByText('(McLaren)')).toHaveLength(2)
    expect(screen.getByText('P1')).toBeInTheDocument()

    expect(screen.getByText('Oscar Piastri')).toBeInTheDocument()
    expect(screen.getByText('P2')).toBeInTheDocument()

    expect(screen.getByText('Max Verstappen')).toBeInTheDocument()
    expect(screen.getByText('(Red Bull)')).toBeInTheDocument()
    expect(screen.getByText('P3')).toBeInTheDocument()
  })

  it('displays win probabilities as percentages', () => {
    render(<WinProbabilityWidget entries={sampleEntries} />)

    expect(screen.getByText('52.3%')).toBeInTheDocument()
    expect(screen.getByText('28.7%')).toBeInTheDocument()
    expect(screen.getByText('19.0%')).toBeInTheDocument()
  })

  it('renders a list item for each entry', () => {
    render(<WinProbabilityWidget entries={sampleEntries} />)

    expect(screen.getAllByRole('listitem')).toHaveLength(3)
  })
})
