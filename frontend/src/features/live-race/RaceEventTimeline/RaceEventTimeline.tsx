import { useLiveRaceStore } from '../store/liveRaceStore'
import type { RaceTimelineEvent } from '../../../shared/types/f1'

const EVENT_STYLE: Record<RaceTimelineEvent['eventType'], { label: string; colour: string }> = {
  SafetyCar: { label: 'SC', colour: '#ffcc00' },
  VirtualSafetyCar: { label: 'VSC', colour: '#ffe066' },
  RedFlag: { label: 'RED', colour: '#e8002d' },
  PitStop: { label: 'PIT', colour: '#3671c6' },
  Dnf: { label: 'DNF', colour: '#6b7280' },
  FastestLap: { label: 'FL', colour: '#bf00ff' },
}

export function RaceEventTimeline() {
  const timeline = useLiveRaceStore(s => s.timeline)

  if (timeline.length === 0) {
    return (
      <div
        className="px-3 py-4 text-[12px] text-[#6b7280] bg-[#20242c] rounded-[8px]"
        data-testid="race-event-timeline-empty"
      >
        No race events yet
      </div>
    )
  }

  const maxLap = Math.max(1, ...timeline.map(e => e.lapNumber))

  return (
    <div className="relative h-16 bg-[#20242c] rounded-[8px] px-2" data-testid="race-event-timeline">
      <div className="absolute inset-x-2 top-1/2 h-px bg-[#3a4050]" />
      {timeline.map((event, i) => {
        const pct = (event.lapNumber / maxLap) * 100
        const style = EVENT_STYLE[event.eventType]
        return (
          <div
            key={i}
            className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 flex flex-col items-center"
            style={{ left: `${pct}%` }}
            data-testid={`timeline-event-${event.eventType}`}
            title={`Lap ${event.lapNumber}${event.driverCode ? ` — ${event.driverCode}` : ''}`}
          >
            <span className="w-2 h-2 rounded-full" style={{ backgroundColor: style.colour }} />
            <span className="text-[9px] text-[#9aa1ad] mt-0.5">{style.label}</span>
          </div>
        )
      })}
    </div>
  )
}
