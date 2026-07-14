import { Link, useParams } from 'react-router-dom'
import { useCircuitProfile } from '../../shared/api/ergast'
import { CircuitTrackLayout } from './CircuitTrackLayout'

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

export function CircuitProfilePage() {
  const { circuitId } = useParams<{ circuitId: string }>()
  const { data, isPending, isError } = useCircuitProfile(circuitId)

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <Link
        to="/"
        className="mb-6 inline-block text-[13px] text-text-secondary hover:text-accent-editorial"
      >
        ← Calendar
      </Link>

      {isPending && <ProfileSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}
      {data === null && (
        <p className="text-[13px] text-text-secondary">Circuit not found.</p>
      )}

      {data && (
        <>
          <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">{data.circuitName}</h1>
          <p className="mb-7 text-[13px] text-text-secondary">
            {data.locality}, {data.country} · First F1 race in {data.firstF1Season}
          </p>

          <section className="mb-7">
            <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
              Track Layout
            </h2>
            <CircuitTrackLayout circuitId={data.circuitId} />
          </section>

          <section className="mb-7 grid grid-cols-2 gap-4">
            <div className="rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
              <h2 className="mb-2 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                All-Time Lap Record
              </h2>
              {data.lapRecord ? (
                <>
                  <p className="text-[15px] font-bold text-text-primary">{data.lapRecord.driverName}</p>
                  <p className="text-[13px] text-text-secondary">
                    {data.lapRecord.constructorName} · {data.lapRecord.time} · {data.lapRecord.season}
                  </p>
                </>
              ) : (
                <p className="text-[13px] text-text-secondary">No lap record data available.</p>
              )}
            </div>

            <div className="rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
              <h2 className="mb-2 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                Circuit Stats
              </h2>
              {data.stats ? (
                <p className="text-[13px] text-text-secondary">
                  {data.stats.lengthKm} km · {data.stats.corners} corners · {data.stats.drsZones} DRS zone
                  {data.stats.drsZones === 1 ? '' : 's'}
                </p>
              ) : (
                <p className="text-[13px] text-text-secondary">Circuit stats not available.</p>
              )}
            </div>
          </section>

          <section>
            <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
              Past Winners
            </h2>
            {data.pastWinners.length === 0 ? (
              <p className="text-[13px] text-text-secondary">No past races at this circuit.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full border-collapse text-[13px]">
                  <thead>
                    <tr>
                      <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                        Year
                      </th>
                      <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                        Driver
                      </th>
                      <th className="border-b border-border-soft px-2.5 py-2 text-left text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
                        Constructor
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.pastWinners.map((winner) => (
                      <tr key={winner.season} className="hover:bg-bg-card-hover">
                        <td className="border-b border-bg-card-hover px-2.5 py-[11px] font-semibold text-text-tertiary">
                          {winner.season}
                        </td>
                        <td className="border-b border-bg-card-hover px-2.5 py-[11px] font-semibold text-text-primary">
                          {winner.driverName}
                        </td>
                        <td className="border-b border-bg-card-hover px-2.5 py-[11px] text-text-secondary">
                          {winner.constructorName}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  )
}
