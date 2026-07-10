import { useLiveRaceStore } from '../store/liveRaceStore'
import type { FastestSectorEntry } from '../../../shared/types/f1'

function SectorCell({ label, entry }: { label: 'S1' | 'S2' | 'S3'; entry: FastestSectorEntry | null }) {
  return (
    <div
      className="flex flex-col items-center gap-1 px-3 py-2 rounded-[8px] bg-[#20242c] min-w-[64px]"
      data-testid={`sector-${label.toLowerCase()}`}
    >
      <span className="text-[10px] text-[#6b7280] uppercase tracking-wide">{label}</span>
      {entry ? (
        <>
          <span
            className="text-[14px] font-bold text-[#bf00ff] tabular-nums"
            data-testid={`sector-${label.toLowerCase()}-time`}
          >
            {entry.timeSeconds.toFixed(3)}
          </span>
          <span className="text-[11px] text-[#eef0f3]">{entry.driverCode}</span>
        </>
      ) : (
        <span className="text-[12px] text-[#6b7280]">—</span>
      )}
    </div>
  )
}

export function FastestSectorBoard() {
  const sessionMode = useLiveRaceStore(s => s.sessionMode)
  const fastestSectors = useLiveRaceStore(s => s.fastestSectors)

  if (sessionMode === 'fallback') return null

  return (
    <div className="flex gap-2" data-testid="fastest-sector-board">
      <SectorCell label="S1" entry={fastestSectors?.s1 ?? null} />
      <SectorCell label="S2" entry={fastestSectors?.s2 ?? null} />
      <SectorCell label="S3" entry={fastestSectors?.s3 ?? null} />
    </div>
  )
}
