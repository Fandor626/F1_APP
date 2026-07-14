import { useShallow } from 'zustand/react/shallow'
import { useLiveRaceStore } from '../store/liveRaceStore'
import { DriverRow } from './DriverRow'

export function GapList() {
  const connectionStatus = useLiveRaceStore(s => s.connectionStatus)

  const sortedDriverIds = useLiveRaceStore(
    useShallow(s =>
      Object.values(s.drivers)
        .sort((a, b) => a.position - b.position)
        .map(d => String(d.driverNumber))
    )
  )

  return (
    <div className="bg-[#1b1f26] border border-[#2a2f38] rounded-[14px] overflow-hidden">
      <div className="flex items-center justify-between px-[10px] py-[8px] border-b border-[#2a2f38]">
        <div className="flex flex-col">
          <span className="text-[11.5px] font-semibold tracking-[0.04em] uppercase text-[#9aa1ad]">
            Race Order
          </span>
          <span className="text-[9px] text-[#8890a0] tracking-wide">
            pts if race ended now
          </span>
        </div>
        <span
          className={`text-[11.5px] font-semibold ${
            connectionStatus === 'connected'
              ? 'text-[#2ee686]'
              : 'text-[#9aa1ad]'
          }`}
        >
          {connectionStatus === 'connected'
            ? '● Live'
            : connectionStatus === 'reconnecting'
              ? '◌ Reconnecting…'
              : '○ Disconnected'}
        </span>
      </div>

      {sortedDriverIds.length === 0 ? (
        <p className="px-[10px] py-6 text-[12px] text-[#8890a0]">
          Waiting for race data…
        </p>
      ) : (
        <div>
          {sortedDriverIds.map(id => (
            <DriverRow key={id} driverId={id} />
          ))}
        </div>
      )}
    </div>
  )
}
