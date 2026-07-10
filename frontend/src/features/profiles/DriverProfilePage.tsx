import { Link, useParams } from 'react-router-dom'
import { useDriverProfile } from '../../shared/api/ergast'
import { DriverCareerChart } from './DriverCareerChart'

function ProfileSkeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-4">
      {[0, 1, 2].map((key) => (
        <div
          key={key}
          className="h-24 animate-pulse rounded-lg border border-border-soft bg-bg-card motion-reduce:animate-none"
        />
      ))}
    </div>
  )
}

function StatTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-border-soft bg-bg-card px-4 py-3 text-center">
      <div className="text-[22px] font-bold tabular-nums text-text-primary">{value}</div>
      <div className="text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">{label}</div>
    </div>
  )
}

export function DriverProfilePage() {
  const { driverId } = useParams<{ driverId: string }>()
  const { data, isPending, isError } = useDriverProfile(driverId)

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <Link
        to="/standings"
        className="mb-6 inline-block text-[13px] text-text-secondary hover:text-accent-editorial"
      >
        ← Standings
      </Link>

      {isPending && <ProfileSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}
      {data === null && <p className="text-[13px] text-text-secondary">Driver not found.</p>}

      {data && (
        <>
          <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">{data.fullName}</h1>
          <p className="mb-7 text-[13px] text-text-secondary">{data.nationality}</p>

          <section className="mb-7 grid grid-cols-3 gap-3 sm:grid-cols-6">
            <StatTile label="Races" value={data.careerTotals.races} />
            <StatTile label="Wins" value={data.careerTotals.wins} />
            <StatTile label="Podiums" value={data.careerTotals.podiums} />
            <StatTile label="Poles" value={data.careerTotals.poles} />
            <StatTile label="Fastest Laps" value={data.careerTotals.fastestLaps} />
            <StatTile label="Titles" value={data.careerTotals.titles} />
          </section>

          <section className="mb-7">
            <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
              Constructor History
            </h2>
            <table className="w-full border-collapse text-[13px]">
              <thead>
                <tr>
                  <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                    Year
                  </th>
                  <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                    Constructor(s)
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.constructorHistory.map((entry) => (
                  <tr key={entry.season} className="hover:bg-bg-card-hover">
                    <td className="border-b border-bg-card-hover px-2.5 py-[11px] font-semibold text-text-tertiary">
                      {entry.season}
                    </td>
                    <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-text-primary">
                      {entry.constructorNames.join(' / ')}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>

          <DriverCareerChart points={data.careerPoints} isPending={false} isError={false} />
        </>
      )}
    </div>
  )
}
