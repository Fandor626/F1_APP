// Ergast returns nationality as an adjective ("British", "Dutch", "Monegasque"),
// but CountryFlag is keyed on country names ("UK", "Netherlands", "Monaco").
// This translates the common F1 driver/constructor nationalities into the
// country names CountryFlag understands.
export const NATIONALITY_TO_COUNTRY: Record<string, string> = {
  American: 'USA',
  Argentine: 'Argentina',
  Australian: 'Australia',
  Austrian: 'Austria',
  Belgian: 'Belgium',
  Brazilian: 'Brazil',
  British: 'UK',
  Canadian: 'Canada',
  Chinese: 'China',
  Danish: 'Denmark',
  Dutch: 'Netherlands',
  Finnish: 'Finland',
  French: 'France',
  German: 'Germany',
  Hungarian: 'Hungary',
  Italian: 'Italy',
  Japanese: 'Japan',
  Mexican: 'Mexico',
  Monegasque: 'Monaco',
  'New Zealander': 'New Zealand',
  Polish: 'Poland',
  Portuguese: 'Portugal',
  Russian: 'Russia',
  'South African': 'South Africa',
  Spanish: 'Spain',
  Swedish: 'Sweden',
  Swiss: 'Switzerland',
  Thai: 'Thailand',
  Turkish: 'Turkey',
}

export function nationalityToCountry(nationality: string): string | undefined {
  return NATIONALITY_TO_COUNTRY[nationality]
}
