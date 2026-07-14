// DESIGN.md defines team accent tokens for all current constructors. Any
// constructor without a token falls back to a neutral dim dot rather than
// inventing an unofficial color — this keeps the app resilient to the live
// API returning a constructor name string that doesn't exactly match one of
// the curated entries below.
const CONSTRUCTOR_COLOR: Record<string, string> = {
  'Red Bull Racing': 'var(--color-team-redbull)',
  Ferrari: 'var(--color-team-ferrari)',
  Mercedes: 'var(--color-team-mercedes)',
  McLaren: 'var(--color-team-mclaren)',
  'Aston Martin': 'var(--color-team-astonmartin)',
  'Alpine F1 Team': 'var(--color-team-alpine)',
  Williams: 'var(--color-team-williams)',
  'RB F1 Team': 'var(--color-team-rb)',
  Sauber: 'var(--color-team-sauber)',
  'Haas F1 Team': 'var(--color-team-haas)',
}

const CONSTRUCTOR_BADGE_LABEL: Record<string, string> = {
  'Red Bull Racing': 'RB',
  Ferrari: 'FER',
  Mercedes: 'MER',
  McLaren: 'MCL',
  'Aston Martin': 'AM',
  'Alpine F1 Team': 'ALP',
  Williams: 'WIL',
  'RB F1 Team': 'VRB',
  Sauber: 'SAU',
  'Haas F1 Team': 'HAA',
}

export function constructorColor(name: string): string {
  return CONSTRUCTOR_COLOR[name] ?? 'var(--color-text-dim)'
}

export function constructorBadgeLabel(name: string): string {
  return CONSTRUCTOR_BADGE_LABEL[name] ?? name.slice(0, 3).toUpperCase()
}
