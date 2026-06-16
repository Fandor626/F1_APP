// Ergast/Jolpica country names, as currently used across the F1 calendar,
// mapped to ISO 3166-1 alpha-2 so the flag itself can be derived generically
// (regional indicator symbols) instead of needing one emoji per country.
const COUNTRY_NAME_TO_ISO: Record<string, string> = {
  Australia: 'AU',
  Austria: 'AT',
  Azerbaijan: 'AZ',
  Bahrain: 'BH',
  Belgium: 'BE',
  Brazil: 'BR',
  Canada: 'CA',
  China: 'CN',
  France: 'FR',
  Germany: 'DE',
  Hungary: 'HU',
  Italy: 'IT',
  Japan: 'JP',
  Mexico: 'MX',
  Monaco: 'MC',
  Netherlands: 'NL',
  Portugal: 'PT',
  Qatar: 'QA',
  Russia: 'RU',
  'Saudi Arabia': 'SA',
  Singapore: 'SG',
  'South Africa': 'ZA',
  'South Korea': 'KR',
  Spain: 'ES',
  Turkey: 'TR',
  UAE: 'AE',
  UK: 'GB',
  USA: 'US',
}

const REGIONAL_INDICATOR_OFFSET = 127397 // 0x1F1E6 - 'A'.charCodeAt(0)

function isoToFlagEmoji(iso: string): string {
  return iso
    .toUpperCase()
    .replace(/./g, (letter) => String.fromCodePoint(REGIONAL_INDICATOR_OFFSET + letter.charCodeAt(0)))
}

interface CountryFlagProps {
  country: string
  className?: string
}

export function CountryFlag({ country, className }: CountryFlagProps) {
  const iso = COUNTRY_NAME_TO_ISO[country]
  if (!iso) return null

  return (
    <span role="img" aria-label={`${country} flag`} className={className}>
      {isoToFlagEmoji(iso)}
    </span>
  )
}
