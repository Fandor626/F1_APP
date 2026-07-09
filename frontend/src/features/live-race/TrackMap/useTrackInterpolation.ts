import { useEffect, useRef, useState } from 'react'
import type { DriverState } from '../../../shared/types/f1'

export interface InterpolatedPosition {
  driverNumber: number
  driverCode: string
  teamColour: string
  svgX: number
  svgY: number
}

export interface Transform {
  scaleX: number
  scaleY: number
  translateX: number
  translateY: number
  rotationDeg: number
}

function applyTransform(rawX: number, rawY: number, t: Transform): [number, number] {
  const rad = (t.rotationDeg * Math.PI) / 180
  const rotX = rawX * Math.cos(rad) - rawY * Math.sin(rad)
  const rotY = rawX * Math.sin(rad) + rawY * Math.cos(rad)
  return [rotX * t.scaleX + t.translateX, rotY * t.scaleY + t.translateY]
}

interface Sample {
  x: number
  y: number
  receivedAt: number
}

export function useTrackInterpolation(
  drivers: Record<string, DriverState>,
  transform: Transform | null
): InterpolatedPosition[] {
  const samplesRef = useRef<Map<number, [Sample, Sample | null]>>(new Map())
  const [positions, setPositions] = useState<InterpolatedPosition[]>([])
  const rafRef = useRef<number>(0)

  useEffect(() => {
    const now = performance.now()
    for (const driver of Object.values(drivers)) {
      if (driver.x == null || driver.y == null) continue
      const newSample: Sample = { x: driver.x, y: driver.y, receivedAt: now }
      const existing = samplesRef.current.get(driver.driverNumber)
      if (!existing) {
        samplesRef.current.set(driver.driverNumber, [newSample, null])
      } else {
        samplesRef.current.set(driver.driverNumber, [newSample, existing[0]])
      }
    }
  }, [drivers])

  useEffect(() => {
    if (!transform) return

    const tick = () => {
      const now = performance.now()
      const result: InterpolatedPosition[] = []

      for (const driver of Object.values(drivers)) {
        const entry = samplesRef.current.get(driver.driverNumber)
        if (!entry) continue
        const [newest, prev] = entry

        let rawX: number
        let rawY: number

        if (!prev) {
          rawX = newest.x
          rawY = newest.y
        } else {
          const span = newest.receivedAt - prev.receivedAt
          const elapsed = now - prev.receivedAt
          const alpha = span > 0 ? Math.min(1, elapsed / span) : 1
          rawX = prev.x + alpha * (newest.x - prev.x)
          rawY = prev.y + alpha * (newest.y - prev.y)
        }

        const [svgX, svgY] = applyTransform(rawX, rawY, transform)
        result.push({
          driverNumber: driver.driverNumber,
          driverCode: driver.driverCode,
          teamColour: driver.teamColour,
          svgX,
          svgY,
        })
      }

      setPositions(result)
      rafRef.current = requestAnimationFrame(tick)
    }

    rafRef.current = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(rafRef.current)
  }, [drivers, transform])

  return positions
}
