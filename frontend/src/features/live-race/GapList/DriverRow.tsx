import { useLiveRaceStore } from '../store/liveRaceStore'
import { getTyreColour } from '../../../shared/utils/tyreUtils'

interface DriverRowProps {
  driverId: string
}

export function DriverRow({ driverId }: DriverRowProps) {
  const driver = useLiveRaceStore(s => s.drivers[driverId])

  if (!driver) return null

  const isBattle =
    !driver.gapIsStale &&
    driver.gapToCarAhead !== null &&
    parseFloat(driver.gapToCarAhead) < 1.0

  let gapDisplay: React.ReactNode
  if (driver.gapIsStale) {
    gapDisplay = (
      <span className="opacity-50 text-[#9aa1ad]" data-testid="stale-gap">
        ~{driver.gapToCarAhead ?? '–'}
      </span>
    )
  } else if (driver.gapToCarAhead === null) {
    gapDisplay = <span className="text-[#6b7280]">—</span>
  } else if (isBattle) {
    gapDisplay = (
      <span className="text-[#d8b65c] font-semibold" data-testid="battle-gap">
        {driver.gapToCarAhead}
      </span>
    )
  } else {
    gapDisplay = <span>{driver.gapToCarAhead}</span>
  }

  return (
    <div className="flex items-center gap-3 px-[10px] py-[6px] text-[12px] hover:bg-[#20242c] cursor-default font-[Avenir_Next,sans-serif]">
      <span className="w-5 text-right text-[#6b7280] shrink-0">
        {driver.position}
      </span>
      <span
        className="shrink-0 rounded-full"
        style={{
          width: 7,
          height: 7,
          backgroundColor: `#${driver.teamColour}`,
        }}
      />
      <span className="w-8 font-semibold text-[#eef0f3] shrink-0">
        {driver.driverCode}
      </span>
      {driver.tyreCompound !== null ? (
        <span className="flex items-center gap-1 shrink-0">
          <span
            className="rounded-full shrink-0"
            style={{
              width: 10,
              height: 10,
              backgroundColor: getTyreColour(driver.tyreCompound),
            }}
            data-testid="tyre-compound"
            aria-label={driver.tyreCompound}
          />
          {driver.stintLaps !== null && (
            <span className="text-[#9aa1ad] tabular-nums" data-testid="stint-laps">
              {driver.stintLaps}
            </span>
          )}
        </span>
      ) : null}
      {driver.championshipDelta != null && (
        <span
          className="text-[10px] text-[#6b7280] tabular-nums shrink-0"
          data-testid="championship-delta"
        >
          {driver.championshipDelta}
        </span>
      )}
      <span className="ml-auto tabular-nums">{gapDisplay}</span>
    </div>
  )
}
