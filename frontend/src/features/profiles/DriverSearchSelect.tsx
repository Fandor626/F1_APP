import { useMemo, useState } from 'react'
import type { DriverOption } from '../../shared/api/ergast'

interface DriverSearchSelectProps {
  label: string
  drivers: DriverOption[]
  selectedDriverId: string | null
  onSelect: (driverId: string) => void
}

export function DriverSearchSelect({ label, drivers, selectedDriverId, onSelect }: DriverSearchSelectProps) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)

  const selectedDriver = drivers.find((d) => d.driverId === selectedDriverId)

  const matches = useMemo(() => {
    if (!query) return []
    const lower = query.toLowerCase()
    return drivers.filter((d) => d.fullName.toLowerCase().includes(lower)).slice(0, 8)
  }, [drivers, query])

  const inputId = `driver-search-${label.toLowerCase().replace(/\s+/g, '-')}`

  return (
    <div className="relative">
      <label
        htmlFor={inputId}
        className="mb-1 block text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase"
      >
        {label}
      </label>
      <input
        id={inputId}
        type="text"
        role="combobox"
        aria-expanded={isOpen}
        placeholder={selectedDriver?.fullName ?? 'Search a driver…'}
        value={query}
        onChange={(e) => {
          setQuery(e.target.value)
          setIsOpen(true)
        }}
        onFocus={() => setIsOpen(true)}
        onBlur={() => setTimeout(() => setIsOpen(false), 150)}
        className="w-full rounded-md border border-border-soft bg-bg-card px-3 py-2 text-[13px] text-text-primary placeholder:text-text-tertiary focus:border-accent-editorial focus:outline-none"
      />
      {isOpen && matches.length > 0 && (
        <ul className="absolute z-10 mt-1 max-h-56 w-full overflow-y-auto rounded-md border border-border-soft bg-bg-card shadow-lg">
          {matches.map((driver) => (
            <li key={driver.driverId}>
              <button
                type="button"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => {
                  onSelect(driver.driverId)
                  setQuery('')
                  setIsOpen(false)
                }}
                className="block w-full px-3 py-2 text-left text-[13px] text-text-primary hover:bg-bg-card-hover"
              >
                {driver.fullName}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
