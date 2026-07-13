import { useEffect, useState } from 'react'

interface CircuitConfig {
  circuitId: string
  viewBox: string
  trackPath: string
}

interface TrackOutlineProps {
  circuitId: string
  circuitName: string
  className?: string
}

// Decorative track shape, shared by the Calendar card and Race Weekend
// Detail page. Deliberately separate from TrackMap.tsx (the live GPS map) —
// this component has no live-race-state dependency and never renders driver
// dots; it only consumes the trackPath/viewBox fields of the same
// circuit-configs asset (Architecture AD-5).
export function TrackOutline({ circuitId, circuitName, className }: TrackOutlineProps) {
  const [config, setConfig] = useState<CircuitConfig | null>(null)
  const [unavailable, setUnavailable] = useState(false)

  useEffect(() => {
    let cancelled = false

    // Relative same-origin path only — never VITE_API_BASE_URL-prefixed
    // (Story 7.3 / AD-6): circuit-configs is a frontend static asset.
    fetch(`/circuit-configs/${circuitId}.json`)
      .then((r) => {
        if (!r.ok) throw new Error('not found')
        return r.json() as Promise<CircuitConfig>
      })
      .then((data) => {
        if (cancelled) return
        setConfig(data)
        setUnavailable(false)
      })
      .catch(() => {
        if (cancelled) return
        setConfig(null)
        setUnavailable(true)
      })

    return () => {
      cancelled = true
    }
  }, [circuitId])

  if (unavailable || !config) return null

  return (
    <svg
      viewBox={config.viewBox}
      role="img"
      aria-label={`Track layout: ${circuitName}`}
      className={className}
    >
      <path
        d={config.trackPath}
        fill="none"
        stroke="currentColor"
        strokeWidth={28}
        strokeLinejoin="round"
        strokeLinecap="round"
      />
    </svg>
  )
}
