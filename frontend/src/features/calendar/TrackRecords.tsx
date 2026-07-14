import { Link } from 'react-router-dom'
import type { LapRecord } from '../../shared/api/ergast'

interface TrackRecordsProps {
  allTimeLapRecord?: LapRecord | null
  recentLapRecord?: LapRecord | null
}

function LapRecordRow({ label, record }: { label: string; record: LapRecord }) {
  return (
    <div className="flex items-center justify-between rounded-lg border border-border-soft bg-bg-inset px-[22px] py-3">
      <div>
        <div className="text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">{label}</div>
        <Link
          to={`/drivers/${record.driverId}`}
          className="text-[15px] font-bold text-text-primary hover:text-accent-editorial hover:underline"
        >
          {record.driverName}
        </Link>
        <div className="text-[12.5px] text-text-secondary">
          {record.constructorName} · {record.season}
        </div>
      </div>
      <span className="text-[15px] font-bold tabular-nums text-text-primary">{record.time}</span>
    </div>
  )
}

// Deliberately a separate panel from the track-layout panel, per UX-DR8 —
// not merged into one "track" mega-card.
export function TrackRecords({ allTimeLapRecord, recentLapRecord }: TrackRecordsProps) {
  if (!allTimeLapRecord && !recentLapRecord) return null

  return (
    <div className="mb-7">
      <h2 className="mb-3 text-[11.5px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
        Track Records
      </h2>
      <div className="flex flex-col gap-2">
        {allTimeLapRecord && <LapRecordRow label="All-Time Fastest Lap" record={allTimeLapRecord} />}
        {recentLapRecord && <LapRecordRow label={`${recentLapRecord.season} Fastest Lap`} record={recentLapRecord} />}
      </div>
    </div>
  )
}
