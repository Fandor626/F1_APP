// Hand-curated team-principal roster (AD-10) — not sourced from any API.
// Matched against the same constructor-name strings the app already renders
// (see constructorColors.ts). Unknown/unmatched names fail closed to `null`
// rather than showing stale or wrong data.
const TEAM_PRINCIPAL: Record<string, string> = {
  McLaren: 'Andrea Stella',
  Ferrari: 'Fred Vasseur',
  'Red Bull Racing': 'Laurent Mekies',
  Mercedes: 'Toto Wolff',
  'Aston Martin': 'Andy Cowell',
  'Alpine F1 Team': 'Flavio Briatore',
  Williams: 'James Vowles',
  'RB F1 Team': 'Alan Permane',
  Sauber: 'Jonathan Wheatley',
  'Haas F1 Team': 'Ayao Komatsu',
}

export function teamPrincipal(constructorName: string): string | null {
  return TEAM_PRINCIPAL[constructorName] ?? null
}
