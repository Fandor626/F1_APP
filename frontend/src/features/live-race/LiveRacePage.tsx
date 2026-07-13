import { useEffect } from 'react'
import { GapList } from './GapList/GapList'
import { LapTimeChart } from './LapTimeChart/LapTimeChart'
import { TrackMap } from './TrackMap/TrackMap'
import { FastestSectorBoard } from './FastestSectorBoard/FastestSectorBoard'
import { RaceEventTimeline } from './RaceEventTimeline/RaceEventTimeline'
import { ReplayBar } from './ReplayBar/ReplayBar'
import { useSignalRConnection } from './hooks/useSignalRConnection'
import { useFallbackState } from './hooks/useFallbackState'
import { useLastRaceResult } from './hooks/useLastRaceResult'
import { useLiveRaceStore } from './store/liveRaceStore'
import { normalizeSnapshot } from '../../shared/utils/normalizeSnapshot'
import { useRecordLiveVisit } from '../fan-engagement/useRecordLiveVisit'
import { useRaceSchedule } from '../../shared/api/ergast'
import { formatDateRange } from '../../shared/utils/dateUtils'

export function LiveRacePage() {
  useSignalRConnection()
  useRecordLiveVisit()

  const { isFallback, isStale, fallbackRaceName } = useFallbackState()
  const drivers = useLiveRaceStore(s => s.drivers)
  const circuitId = useLiveRaceStore(s => s.circuitId)
  const setDrivers = useLiveRaceStore(s => s.setDrivers)
  const setFallbackRaceName = useLiveRaceStore(s => s.setFallbackRaceName)

  const hasLiveData = Object.keys(drivers).length > 0

  // Enabled whenever fallback (not just "no live data yet"): the Replay bar
  // needs season/round regardless of whether SignalR or this REST call ends
  // up being the one that actually populates the driver list below — SignalR
  // can win that race before this query ever fires otherwise, leaving
  // season/round unavailable for the whole fallback session.
  const { data: lastRaceData, isFetched: lastRaceFetched } = useLastRaceResult({ enabled: isFallback })
  const { data: schedule } = useRaceSchedule()

  useEffect(() => {
    if (lastRaceData && isFallback && !hasLiveData) {
      setDrivers(normalizeSnapshot(lastRaceData.drivers))
      setFallbackRaceName(lastRaceData.raceName)
    }
  }, [lastRaceData, isFallback, hasLiveData, setDrivers, setFallbackRaceName])

  // No race has completed yet this season — REST confirmed it (204, resolved
  // to null) and SignalR hasn't delivered anything either (the orchestrator
  // never broadcasts a snapshot with zero drivers). Show a plain on-brand
  // message naming the next race instead of a page that merely looks empty.
  const noCompletedRacesYet = isFallback && !hasLiveData && lastRaceFetched && lastRaceData === null

  if (noCompletedRacesYet) {
    const nextRace = schedule?.find(race => new Date(race.raceStart) >= new Date())
    return (
      <div className="min-h-screen bg-[#14171c] text-[#eef0f3] p-4">
        <h1 className="text-[26px] font-bold tracking-[-0.01em] mb-4">Live Race</h1>
        <p className="text-[13px] text-[#9aa1ad]" data-testid="no-races-yet">
          No races completed yet this season
          {nextRace &&
            ` — first race: ${nextRace.raceName}, ${formatDateRange(new Date(nextRace.raceStart), new Date(nextRace.raceStart))}`}
        </p>
      </div>
    )
  }

  // Replay is only meaningful once there's a real fallback race with a known
  // season/round to fetch — not during live/stale sessions.
  const showReplayBar = isFallback && hasLiveData && lastRaceData

  return (
    <div className={`min-h-screen bg-[#14171c] text-[#eef0f3] p-4 ${showReplayBar ? 'pb-[92px]' : ''}`}>
      <h1 className="text-[26px] font-bold tracking-[-0.01em] mb-4">Live Race</h1>
      {isFallback && (
        <div
          className="mb-4 px-3 py-2 bg-[#2a2f38] rounded-[8px] text-[12px] text-[#9aa1ad] border border-[#3a4050]"
          data-testid="fallback-banner"
        >
          📺 Past Race — {fallbackRaceName ?? 'Last Race'}
        </div>
      )}
      {isStale && (
        <div
          className="mb-4 px-3 py-2 bg-[#2a1f0e] rounded-[8px] text-[12px] text-[#d8b65c] border border-[#4a3a1a]"
          data-testid="stale-banner"
        >
          ⚠️ Connection unstable — data may be delayed
        </div>
      )}
      <div className="flex flex-col gap-4">
        <GapList />
        <FastestSectorBoard />
        <RaceEventTimeline />
        <TrackMap circuitId={circuitId} />
        <LapTimeChart />
      </div>
      {showReplayBar && <ReplayBar season={lastRaceData.season} round={lastRaceData.round} />}
    </div>
  )
}
