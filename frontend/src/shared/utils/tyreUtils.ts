// Standard F1 tyre compound colours (Pirelli official palette)
export const TYRE_COLOURS: Record<string, string> = {
  SOFT: '#E8002D',
  MEDIUM: '#FFF200',
  HARD: '#FFFFFF',
  INTERMEDIATE: '#39B54A',
  WET: '#0067FF',
}

// Single-letter abbreviations for compact display
export const TYRE_ABBREVIATIONS: Record<string, string> = {
  SOFT: 'S',
  MEDIUM: 'M',
  HARD: 'H',
  INTERMEDIATE: 'I',
  WET: 'W',
}

export function getTyreColour(compound: string | null): string {
  if (!compound) return '#6b7280'
  return TYRE_COLOURS[compound.toUpperCase()] ?? '#6b7280'
}

export function getTyreAbbreviation(compound: string | null): string {
  if (!compound) return '?'
  return TYRE_ABBREVIATIONS[compound.toUpperCase()] ?? compound[0].toUpperCase()
}
