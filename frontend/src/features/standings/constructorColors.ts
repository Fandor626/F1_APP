// DESIGN.md defines team accent tokens for a handful of constructors only
// ("constructor accents used sparingly") — any constructor without a token
// falls back to a neutral dim dot rather than inventing an unofficial color.
const CONSTRUCTOR_COLOR: Record<string, string> = {
  'Red Bull Racing': 'var(--color-team-redbull)',
  Ferrari: 'var(--color-team-ferrari)',
  Mercedes: 'var(--color-team-mercedes)',
  McLaren: 'var(--color-team-mclaren)',
}

export function constructorColor(name: string): string {
  return CONSTRUCTOR_COLOR[name] ?? 'var(--color-text-dim)'
}
