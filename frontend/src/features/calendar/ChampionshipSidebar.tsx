import { useState } from 'react'
import { useConstructorStandings, useDriverStandings } from '../../shared/api/ergast'

interface StandingRow {
  position: number
  name: string
  points: number
}

function StandingsList({ title, rows }: { title: string; rows: StandingRow[] }) {
  return (
    <div>
      <div className="mb-2 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
        {title}
      </div>
      {rows.map((row) => (
        <div key={row.position} className="flex items-center justify-between py-1 text-[13px]">
          <span className="flex items-center gap-2">
            <span className="w-3.5 text-[12px] text-text-tertiary">{row.position}</span>
            <span className="text-text-primary">{row.name}</span>
          </span>
          <span className="text-[12.5px] text-text-secondary tabular-nums">{row.points} pts</span>
        </div>
      ))}
    </div>
  )
}

export function ChampionshipSidebar() {
  const { data: driverStandings } = useDriverStandings()
  const { data: constructorStandings } = useConstructorStandings()
  const [mobileExpanded, setMobileExpanded] = useState(false)

  if (!driverStandings || !constructorStandings) return null

  const content = (
    <div className="flex flex-col gap-5">
      <StandingsList
        title="Drivers' Championship"
        rows={driverStandings
          .slice(0, 3)
          .map((standing) => ({ position: standing.position, name: standing.driverName, points: standing.points }))}
      />
      <StandingsList
        title="Constructors' Championship"
        rows={constructorStandings
          .slice(0, 3)
          .map((standing) => ({ position: standing.position, name: standing.constructorName, points: standing.points }))}
      />
    </div>
  )

  return (
    <aside aria-label="Championship standings" className="md:sticky md:top-8 md:self-start">
      <button
        type="button"
        onClick={() => setMobileExpanded((expanded) => !expanded)}
        aria-expanded={mobileExpanded}
        aria-controls="championship-sidebar-content"
        className="mb-4 flex w-full items-center justify-between rounded-md bg-bg-inset px-4 py-3 text-[13px] font-semibold text-text-primary md:hidden"
      >
        Championship standings
        <span aria-hidden="true">{mobileExpanded ? '−' : '+'}</span>
      </button>

      <div
        id="championship-sidebar-content"
        className={`rounded-lg bg-bg-inset p-5 md:block ${mobileExpanded ? 'block' : 'hidden'}`}
      >
        {content}
      </div>
    </aside>
  )
}
