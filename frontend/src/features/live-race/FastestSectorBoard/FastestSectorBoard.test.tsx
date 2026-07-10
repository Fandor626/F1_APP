import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { FastestSectorBoard } from './FastestSectorBoard'
import { useLiveRaceStore } from '../store/liveRaceStore'

beforeEach(() => {
  useLiveRaceStore.setState({
    sessionMode: 'live',
    fastestSectors: null,
  })
})

describe('FastestSectorBoard', () => {
  it('shows placeholder dash for a sector with no data yet', () => {
    render(<FastestSectorBoard />)
    expect(screen.getByTestId('sector-s1').textContent).toContain('—')
    expect(screen.getByTestId('sector-s2').textContent).toContain('—')
    expect(screen.getByTestId('sector-s3').textContent).toContain('—')
  })

  it('shows time and driver code for a sector with a holder', () => {
    useLiveRaceStore.setState({
      fastestSectors: {
        s1: { driverNumber: 44, driverCode: 'HAM', teamColour: '00D2BE', timeSeconds: 28.123 },
        s2: null,
        s3: null,
      },
    })
    render(<FastestSectorBoard />)
    expect(screen.getByTestId('sector-s1-time').textContent).toBe('28.123')
    expect(screen.getByTestId('sector-s1').textContent).toContain('HAM')
  })

  it('returns null when sessionMode is fallback', () => {
    useLiveRaceStore.setState({ sessionMode: 'fallback' })
    const { container } = render(<FastestSectorBoard />)
    expect(container.firstChild).toBeNull()
  })

  it('updates to the new time/holder when fastestSectors changes', () => {
    useLiveRaceStore.setState({
      fastestSectors: {
        s1: { driverNumber: 44, driverCode: 'HAM', teamColour: '00D2BE', timeSeconds: 28.5 },
        s2: null,
        s3: null,
      },
    })
    const { rerender } = render(<FastestSectorBoard />)
    expect(screen.getByTestId('sector-s1-time').textContent).toBe('28.500')

    useLiveRaceStore.setState({
      fastestSectors: {
        s1: { driverNumber: 1, driverCode: 'VER', teamColour: '3671C6', timeSeconds: 28.1 },
        s2: null,
        s3: null,
      },
    })
    rerender(<FastestSectorBoard />)

    expect(screen.getByTestId('sector-s1-time').textContent).toBe('28.100')
    expect(screen.getByTestId('sector-s1').textContent).toContain('VER')
  })
})
