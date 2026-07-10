import { useState } from 'react'
import { ConstructorsStandingsTable } from './ConstructorsStandingsTable'
import { DriversStandingsTable } from './DriversStandingsTable'
import { SeasonWrapped } from './SeasonWrapped/SeasonWrapped'
import { TrajectoryChart } from './TrajectoryChart'

type StandingsTab = 'drivers' | 'constructors'

export function StandingsPage() {
  const [tab, setTab] = useState<StandingsTab>('drivers')

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">Standings</h1>
      <p className="mb-7 text-[13px] text-text-secondary">Drivers' and Constructors' Championships.</p>

      <div className="mb-6 inline-flex rounded-md border border-border-soft bg-bg-card p-[3px]">
        <button
          type="button"
          onClick={() => setTab('drivers')}
          aria-pressed={tab === 'drivers'}
          className={`rounded px-5 py-2 text-[13px] font-semibold transition-colors ${
            tab === 'drivers' ? 'bg-bg-card-hover text-text-primary' : 'text-text-secondary hover:text-text-primary'
          }`}
        >
          Drivers
        </button>
        <button
          type="button"
          onClick={() => setTab('constructors')}
          aria-pressed={tab === 'constructors'}
          className={`rounded px-5 py-2 text-[13px] font-semibold transition-colors ${
            tab === 'constructors' ? 'bg-bg-card-hover text-text-primary' : 'text-text-secondary hover:text-text-primary'
          }`}
        >
          Constructors
        </button>
      </div>

      <div className="rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
        <div className="mb-3.5 text-[15px] font-bold tracking-[-0.01em] text-text-primary">
          {tab === 'drivers' ? "Drivers' Championship" : "Constructors' Championship"}
        </div>
        {tab === 'drivers' ? <DriversStandingsTable /> : <ConstructorsStandingsTable />}
      </div>

      <TrajectoryChart />
      <SeasonWrapped />
    </div>
  )
}
