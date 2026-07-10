import { useEffect, useState } from 'react'

interface CircuitConfig {
  circuitId: string
  viewBox: string
  trackPath: string
}

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

interface CircuitTrackLayoutProps {
  circuitId: string
}

// Reuses the same /circuit-configs/{circuitId}.json asset Epic 3's live
// TrackMap fetches — just renders the static outline, no live telemetry or
// interpolation. Only Monza is calibrated today; every other circuit falls
// back to the same "unavailable" state TrackMap already established.
export function CircuitTrackLayout({ circuitId }: CircuitTrackLayoutProps) {
  const [config, setConfig] = useState<CircuitConfig | null>(null)
  const [unavailable, setUnavailable] = useState(false)

  useEffect(() => {
    setConfig(null)
    setUnavailable(false)

    fetch(`${apiBase}/circuit-configs/${circuitId}.json`)
      .then((r) => {
        if (!r.ok) throw new Error('not found')
        return r.json() as Promise<CircuitConfig>
      })
      .then(setConfig)
      .catch(() => setUnavailable(true))
  }, [circuitId])

  if (unavailable) {
    return (
      <div
        className="flex h-[200px] items-center justify-center rounded-lg border border-border-soft bg-bg-inset text-[12px] text-text-tertiary"
        data-testid="track-layout-unavailable"
      >
        Track layout unavailable for this circuit
      </div>
    )
  }

  if (!config) {
    return (
      <div className="flex h-[200px] items-center justify-center rounded-lg border border-border-soft bg-bg-inset text-[12px] text-text-tertiary">
        Loading…
      </div>
    )
  }

  return (
    <div className="rounded-lg border border-border-soft bg-bg-inset p-2" data-testid="track-layout">
      <svg viewBox={config.viewBox} className="w-full" style={{ maxHeight: 230 }}>
        <path
          d={config.trackPath}
          fill="none"
          stroke="var(--color-text-secondary)"
          strokeWidth={9}
          strokeLinejoin="round"
          strokeLinecap="round"
        />
      </svg>
    </div>
  )
}
