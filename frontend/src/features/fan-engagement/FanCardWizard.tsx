import { useMemo, useState } from 'react'
import { useConstructorStandings, useDriverStandings, useRaceSchedule } from '../../shared/api/ergast'
import { useFanCardStore } from './useFanCardStore'

interface FanCardWizardProps {
  onDone: () => void
}

export function FanCardWizard({ onDone }: FanCardWizardProps) {
  const { data: drivers } = useDriverStandings()
  const { data: constructors } = useConstructorStandings()
  const { data: schedule } = useRaceSchedule()

  const circuits = useMemo(() => {
    if (!schedule) return []
    const seen = new Set<string>()
    return schedule.filter((race) => {
      if (seen.has(race.circuitId)) return false
      seen.add(race.circuitId)
      return true
    })
  }, [schedule])

  const addCard = useFanCardStore((s) => s.addCard)

  const [driverId, setDriverId] = useState('')
  const [constructorName, setConstructorName] = useState('')
  const [circuitId, setCircuitId] = useState('')

  const canSave = !!driverId && !!constructorName && !!circuitId

  function handleSave() {
    const driver = drivers?.find((d) => d.driverId === driverId)
    const circuit = circuits.find((c) => c.circuitId === circuitId)
    if (!driver || !circuit) return

    addCard({
      driverId: driver.driverId,
      driverName: driver.driverName,
      constructorName,
      circuitId: circuit.circuitId,
      circuitName: circuit.circuitName,
    })
    onDone()
  }

  return (
    <div className="rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
      <h2 className="mb-4 text-[15px] font-bold tracking-[-0.01em] text-text-primary">Set Up Your Fan Card</h2>

      <div className="mb-4">
        <label htmlFor="fan-card-driver" className="mb-1 block text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
          Favourite Driver
        </label>
        <select
          id="fan-card-driver"
          value={driverId}
          onChange={(e) => setDriverId(e.target.value)}
          className="w-full rounded-md border border-border-soft bg-bg-inset px-3 py-2 text-[13px] text-text-primary focus:border-accent-editorial focus:outline-none"
        >
          <option value="">Select a driver…</option>
          {drivers?.map((d) => (
            <option key={d.driverId} value={d.driverId}>
              {d.driverName}
            </option>
          ))}
        </select>
      </div>

      <div className="mb-4">
        <label htmlFor="fan-card-constructor" className="mb-1 block text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
          Favourite Constructor
        </label>
        <select
          id="fan-card-constructor"
          value={constructorName}
          onChange={(e) => setConstructorName(e.target.value)}
          className="w-full rounded-md border border-border-soft bg-bg-inset px-3 py-2 text-[13px] text-text-primary focus:border-accent-editorial focus:outline-none"
        >
          <option value="">Select a constructor…</option>
          {constructors?.map((c) => (
            <option key={c.constructorName} value={c.constructorName}>
              {c.constructorName}
            </option>
          ))}
        </select>
      </div>

      <div className="mb-6">
        <label htmlFor="fan-card-circuit" className="mb-1 block text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
          Favourite Circuit
        </label>
        <select
          id="fan-card-circuit"
          value={circuitId}
          onChange={(e) => setCircuitId(e.target.value)}
          className="w-full rounded-md border border-border-soft bg-bg-inset px-3 py-2 text-[13px] text-text-primary focus:border-accent-editorial focus:outline-none"
        >
          <option value="">Select a circuit…</option>
          {circuits.map((c) => (
            <option key={c.circuitId} value={c.circuitId}>
              {c.circuitName}
            </option>
          ))}
        </select>
      </div>

      <button
        type="button"
        onClick={handleSave}
        disabled={!canSave}
        className="rounded-md border border-accent-editorial bg-accent-editorial px-4 py-2 text-[13px] font-semibold text-bg-app disabled:cursor-not-allowed disabled:opacity-50"
      >
        Save Fan Card
      </button>
    </div>
  )
}
