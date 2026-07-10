import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { RaceEventTimeline } from './RaceEventTimeline'
import { useLiveRaceStore } from '../store/liveRaceStore'
import type { RaceTimelineEvent } from '../../../shared/types/f1'

beforeEach(() => {
  useLiveRaceStore.setState({ timeline: [] })
})

describe('RaceEventTimeline', () => {
  it('shows empty state when no events', () => {
    render(<RaceEventTimeline />)
    expect(screen.getByTestId('race-event-timeline-empty')).toBeInTheDocument()
  })

  it('renders a marker for each event type', () => {
    const timeline: RaceTimelineEvent[] = [
      { lapNumber: 1, eventType: 'SafetyCar', driverCode: null, detail: null },
      { lapNumber: 2, eventType: 'VirtualSafetyCar', driverCode: null, detail: null },
      { lapNumber: 3, eventType: 'RedFlag', driverCode: null, detail: null },
      { lapNumber: 4, eventType: 'PitStop', driverCode: 'VER', detail: null },
      { lapNumber: 5, eventType: 'Dnf', driverCode: 'HAM', detail: null },
      { lapNumber: 6, eventType: 'FastestLap', driverCode: 'NOR', detail: null },
    ]
    useLiveRaceStore.setState({ timeline })
    render(<RaceEventTimeline />)

    expect(screen.getByTestId('timeline-event-SafetyCar')).toBeInTheDocument()
    expect(screen.getByTestId('timeline-event-VirtualSafetyCar')).toBeInTheDocument()
    expect(screen.getByTestId('timeline-event-RedFlag')).toBeInTheDocument()
    expect(screen.getByTestId('timeline-event-PitStop')).toBeInTheDocument()
    expect(screen.getByTestId('timeline-event-Dnf')).toBeInTheDocument()
    expect(screen.getByTestId('timeline-event-FastestLap')).toBeInTheDocument()
  })

  it('positions later-lap events further right', () => {
    useLiveRaceStore.setState({
      timeline: [
        { lapNumber: 5, eventType: 'SafetyCar', driverCode: null, detail: null },
        { lapNumber: 50, eventType: 'PitStop', driverCode: 'VER', detail: null },
      ],
    })
    render(<RaceEventTimeline />)

    const earlyLeft = parseFloat(screen.getByTestId('timeline-event-SafetyCar').style.left)
    const lateLeft = parseFloat(screen.getByTestId('timeline-event-PitStop').style.left)

    expect(lateLeft).toBeGreaterThan(earlyLeft)
  })
})
