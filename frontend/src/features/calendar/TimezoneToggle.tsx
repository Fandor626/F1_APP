type TimezoneMode = 'local' | 'track'

interface TimezoneToggleProps {
  mode: TimezoneMode
  onToggle: () => void
}

export function TimezoneToggle({ mode, onToggle }: TimezoneToggleProps) {
  return (
    <div className="flex items-center gap-1">
      <button
        type="button"
        onClick={mode === 'track' ? onToggle : undefined}
        aria-pressed={mode === 'local'}
        aria-disabled={mode === 'local'}
        tabIndex={mode === 'local' ? -1 : 0}
        className={`rounded px-2 py-0.5 text-[12px] font-semibold transition-colors ${
          mode === 'local'
            ? 'bg-accent-editorial text-white'
            : 'text-text-secondary hover:text-text-primary'
        }`}
      >
        Local
      </button>
      <span className="text-[11px] text-text-tertiary">·</span>
      <button
        type="button"
        onClick={mode === 'local' ? onToggle : undefined}
        aria-pressed={mode === 'track'}
        aria-disabled={mode === 'track'}
        tabIndex={mode === 'track' ? -1 : 0}
        className={`rounded px-2 py-0.5 text-[12px] font-semibold transition-colors ${
          mode === 'track'
            ? 'bg-accent-editorial text-white'
            : 'text-text-secondary hover:text-text-primary'
        }`}
      >
        Track
      </button>
    </div>
  )
}
