import { useSeasonWrapped } from '../../../shared/api/ergast'
import { SeasonWrappedCard } from './SeasonWrappedCard'

// Season Wrapped is absent entirely while the season is in progress — not a
// disabled/teaser placeholder (EXPERIENCE.md's explicit state-pattern rule).
export function SeasonWrapped() {
  const { data } = useSeasonWrapped()

  if (!data) return null

  return <SeasonWrappedCard wrapped={data} />
}
