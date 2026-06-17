import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ContextualData } from './ContextualData'

describe('ContextualData', () => {
  it('shows last year\'s winner with driver, team, and time when present', () => {
    render(
      <ContextualData
        priorYearWinner={{ driverName: 'Oscar Piastri', constructorName: 'McLaren', time: '1:35:39.435' }}
      />,
    )

    expect(screen.getByText('Oscar Piastri')).toBeInTheDocument()
    expect(screen.getByText('(McLaren)')).toBeInTheDocument()
    expect(screen.getByText(/1:35:39\.435/)).toBeInTheDocument()
  })

  it('shows "First race at this circuit." when there is no prior-year winner', () => {
    render(<ContextualData />)

    expect(screen.getByText('First race at this circuit.')).toBeInTheDocument()
  })

  it('renders the championship delta with leader, runner-up, and points gap', () => {
    render(
      <ContextualData championshipDelta={{ leaderName: 'Lando Norris', runnerUpName: 'Max Verstappen', pointsGap: 23 }} />,
    )

    expect(screen.getByText('Lando Norris')).toBeInTheDocument()
    expect(screen.getByText('Max Verstappen')).toBeInTheDocument()
    expect(screen.getByText('23')).toBeInTheDocument()
  })

  it('omits the championship gap section when there is no delta', () => {
    render(<ContextualData />)

    expect(screen.queryByText('Championship Gap')).not.toBeInTheDocument()
  })
})
