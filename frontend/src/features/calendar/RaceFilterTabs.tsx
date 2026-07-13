import { useRef } from 'react'
import type { KeyboardEvent } from 'react'

export type RaceFilter = 'all' | 'future' | 'past'

const FILTERS: { value: RaceFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'future', label: 'Future' },
  { value: 'past', label: 'Past' },
]

interface RaceFilterTabsProps {
  value: RaceFilter
  onChange: (filter: RaceFilter) => void
}

export function RaceFilterTabs({ value, onChange }: RaceFilterTabsProps) {
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([])

  function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return
    event.preventDefault()
    const direction = event.key === 'ArrowRight' ? 1 : -1
    const nextIndex = (index + direction + FILTERS.length) % FILTERS.length
    onChange(FILTERS[nextIndex].value)
    tabRefs.current[nextIndex]?.focus()
  }

  return (
    <div
      role="tablist"
      aria-label="Race filter"
      className="mb-6 inline-flex rounded-md border border-border-soft bg-bg-card p-[3px]"
    >
      {FILTERS.map((filter, index) => {
        const selected = filter.value === value
        return (
          <button
            key={filter.value}
            ref={(el) => {
              tabRefs.current[index] = el
            }}
            type="button"
            role="tab"
            aria-selected={selected}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(filter.value)}
            onKeyDown={(event) => handleKeyDown(event, index)}
            className={`rounded px-5 py-2 text-[13px] font-semibold transition-colors ${
              selected ? 'bg-bg-card-hover text-text-primary' : 'text-text-secondary hover:text-text-primary'
            }`}
          >
            {filter.label}
          </button>
        )
      })}
    </div>
  )
}
