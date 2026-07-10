import { useConstructorStandings } from '../../shared/api/ergast'
import { CountryFlag } from '../../shared/components/CountryFlag'
import { constructorColor } from './constructorColors'
import { nationalityToCountry } from './nationalityToCountry'

function ConstructorsStandingsSkeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-2">
      {[0, 1, 2, 3, 4].map((key) => (
        <div
          key={key}
          className="h-9 animate-pulse rounded bg-bg-card-hover motion-reduce:animate-none"
        />
      ))}
    </div>
  )
}

export function ConstructorsStandingsTable() {
  const { data, isPending, isError } = useConstructorStandings()

  if (isPending) return <ConstructorsStandingsSkeleton />
  if (isError) {
    return (
      <p role="alert" className="text-[13px] text-text-secondary">
        Couldn't reach the server — try refreshing.
      </p>
    )
  }

  return (
    <table className="w-full border-collapse text-[13px]">
      <thead>
        <tr>
          <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Pos
          </th>
          <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Constructor
          </th>
          <th className="border-b border-border-soft px-2.5 py-2 text-right text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Points
          </th>
          <th className="border-b border-border-soft px-2.5 py-2 text-right text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Wins
          </th>
        </tr>
      </thead>
      <tbody>
        {data?.map((standing) => {
          const country = nationalityToCountry(standing.nationality)
          return (
            <tr key={standing.position} className="hover:bg-bg-card-hover">
              <td className="border-b border-bg-card-hover px-2.5 py-[11px] font-semibold text-text-tertiary">
                {standing.position}
              </td>
              <td className="border-b border-bg-card-hover px-2.5 py-[11px]">
                <div className="flex items-center gap-2">
                  <span
                    aria-hidden="true"
                    className="h-[7px] w-[7px] flex-shrink-0 rounded-full"
                    style={{ backgroundColor: constructorColor(standing.constructorName) }}
                  />
                  {country && <CountryFlag country={country} className="mr-0.5" />}
                  <span className="font-semibold text-text-primary">{standing.constructorName}</span>
                </div>
              </td>
              <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-right tabular-nums text-text-secondary">
                {standing.points}
              </td>
              <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-right tabular-nums text-text-secondary">
                {standing.wins}
              </td>
            </tr>
          )
        })}
      </tbody>
    </table>
  )
}
