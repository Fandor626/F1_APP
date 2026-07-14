import { useEffect, useState } from 'react'
import { useLiveRaceStore } from '../store/liveRaceStore'
import { useFallbackState } from '../hooks/useFallbackState'
import { useTrackInterpolation } from './useTrackInterpolation'
import { DriverDot } from './DriverDot'
import type { Transform } from './useTrackInterpolation'

interface CircuitConfig {
  circuitId: string
  viewBox: string
  transform?: Transform
  trackPath: string
}

interface TrackMapProps {
  circuitId: string | null
}

export function TrackMap({ circuitId }: TrackMapProps) {
  const drivers = useLiveRaceStore(s => s.drivers)
  const { isFallback } = useFallbackState()

  const [config, setConfig] = useState<CircuitConfig | null>(null)
  const [unavailable, setUnavailable] = useState(false)

  useEffect(() => {
    if (!circuitId || isFallback) return
    setConfig(null)
    setUnavailable(false)

    fetch(`/circuit-configs/${circuitId}.json`)
      .then(r => {
        if (!r.ok) throw new Error('not found')
        return r.json() as Promise<CircuitConfig>
      })
      .then(setConfig)
      .catch(() => setUnavailable(true))
  }, [circuitId, isFallback])

  const positions = useTrackInterpolation(isFallback ? {} : drivers, config?.transform ?? null)

  if (isFallback) return null

  return (
    <div
      className="bg-[#1a1e26] border border-[#2c313b] rounded-[12px] overflow-hidden"
      data-testid="track-map"
    >
      <div className="px-4 pt-3 pb-1 flex justify-between items-baseline">
        <span className="text-[11px] font-semibold tracking-[0.04em] uppercase text-[#8890a0]">
          Circuit
        </span>
        {circuitId && (
          <span className="text-[11px] text-[#9aa1ad] capitalize">
            {circuitId.replace(/_/g, ' ')}
          </span>
        )}
      </div>

      {!circuitId || unavailable ? (
        <div
          className="flex items-center justify-center h-[200px] text-[12px] text-[#8890a0]"
          data-testid="track-map-unavailable"
        >
          Track map unavailable for this circuit
        </div>
      ) : !config ? (
        <div className="flex items-center justify-center h-[200px] text-[12px] text-[#8890a0]">
          Loading…
        </div>
      ) : (
        <svg
          viewBox={config.viewBox}
          className="w-full"
          style={{ maxHeight: 230 }}
          data-testid="track-map-svg"
        >
          <path
            d={config.trackPath}
            fill="none"
            stroke="#2c313b"
            strokeWidth={9}
            strokeLinejoin="round"
            strokeLinecap="round"
          />
          {positions.map(p => (
            <DriverDot
              key={p.driverNumber}
              driverCode={p.driverCode}
              teamColour={p.teamColour}
              svgX={p.svgX}
              svgY={p.svgY}
              miniSectorStatus={p.miniSectorStatus}
            />
          ))}
        </svg>
      )}
    </div>
  )
}
