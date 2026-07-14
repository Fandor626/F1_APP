import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { TrackRecords } from './TrackRecords'
import type { LapRecord } from '../../shared/api/ergast'

const allTime: LapRecord = {
  driverId: 'hamilton',
  driverName: 'Lewis Hamilton',
  constructorName: 'Mercedes',
  time: '1:31.447',
  season: 2019,
}

const recent: LapRecord = {
  driverId: 'norris',
  driverName: 'Lando Norris',
  constructorName: 'McLaren',
  time: '1:32.608',
  season: 2026,
}

function renderRecords(props: { allTimeLapRecord?: LapRecord | null; recentLapRecord?: LapRecord | null }) {
  return render(
    <MemoryRouter>
      <TrackRecords {...props} />
    </MemoryRouter>,
  )
}

describe('TrackRecords', () => {
  it('renders both records with driver names linking to their profile pages', () => {
    renderRecords({ allTimeLapRecord: allTime, recentLapRecord: recent })

    expect(screen.getByText('Lewis Hamilton').closest('a')).toHaveAttribute('href', '/drivers/hamilton')
    expect(screen.getByText('Lando Norris').closest('a')).toHaveAttribute('href', '/drivers/norris')
    expect(screen.getByText('1:31.447')).toBeInTheDocument()
    expect(screen.getByText('1:32.608')).toBeInTheDocument()
  })

  it('shows whatever data exists when only one record is available, rather than an error', () => {
    renderRecords({ allTimeLapRecord: allTime, recentLapRecord: null })

    expect(screen.getByText('Lewis Hamilton')).toBeInTheDocument()
    expect(screen.queryByText('Lando Norris')).not.toBeInTheDocument()
  })

  it('renders nothing when no record data exists at all', () => {
    const { container } = renderRecords({ allTimeLapRecord: null, recentLapRecord: null })

    expect(container.firstChild).toBeNull()
  })
})
