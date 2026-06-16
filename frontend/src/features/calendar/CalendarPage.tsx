import { useRaceSchedule, type RaceWeekend } from '../../shared/api/ergast'
import { RaceWeekendCard } from './RaceWeekendCard'

function splitSchedule(races: RaceWeekend[], now: Date) {
  const next = races.find((race) => new Date(race.raceStart) >= now)
  const rest = next ? races.filter((race) => race !== next) : races
  return { next, rest }
}

function CalendarSkeleton() {
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

function Schedule({ races }: { races: RaceWeekend[] }) {
  const { next, rest } = splitSchedule(races, new Date())

  return (
    <>
      {next && (
        <section className="mb-7">
          <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Next race
          </h2>
          <RaceWeekendCard race={next} isNext />
        </section>
      )}
      <section>
        <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
          Season schedule
        </h2>
        <ul className="flex flex-col gap-4">
          {rest.map((race) => (
            <li key={`${race.season}-${race.round}`}>
              <RaceWeekendCard race={race} />
            </li>
          ))}
        </ul>
      </section>
    </>
  )
}

export function CalendarPage() {
  const { data, isPending, isError } = useRaceSchedule()

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">Race Calendar</h1>
      <p className="mb-7 text-[13px] text-text-secondary">Season schedule. Next race highlighted.</p>

      {isPending && <CalendarSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}
      {data && data.length === 0 && (
        <p className="text-[13px] text-text-secondary">No races scheduled yet.</p>
      )}
      {data && data.length > 0 && <Schedule races={data} />}
    </div>
  )
}
