import { useEffect } from 'react'
import { GapList } from './GapList/GapList'
import { LapTimeChart } from './LapTimeChart/LapTimeChart'
import { TrackMap } from './TrackMap/TrackMap'
import { useSignalRConnection } from './hooks/useSignalRConnection'
import { useFallbackState } from './hooks/useFallbackState'
import { useLastRaceResult } from './hooks/useLastRaceResult'
import { useLiveRaceStore } from './store/liveRaceStore'
import { normalizeSnapshot } from '../../shared/utils/normalizeSnapshot'

export function LiveRacePage() {
  useSignalRConnection()

  const { isFallback, isStale, fallbackRaceName } = useFallbackState()
  const drivers = useLiveRaceStore(s => s.drivers)
  const circuitId = useLiveRaceStore(s => s.circuitId)
  const setDrivers = useLiveRaceStore(s => s.setDrivers)
  const setFallbackRaceName = useLiveRaceStore(s => s.setFallbackRaceName)

  const hasLiveData = Object.keys(drivers).length > 0

  const { data: lastRaceData } = useLastRaceResult({ enabled: isFallback && !hasLiveData })

  useEffect(() => {
    if (lastRaceData && isFallback && !hasLiveData) {
      setDrivers(normalizeSnapshot(lastRaceData.drivers))
      setFallbackRaceName(lastRaceData.raceName)
    }
  }, [lastRaceData, isFallback, hasLiveData, setDrivers, setFallbackRaceName])

  return (
    <div className="min-h-screen bg-[#14171c] text-[#eef0f3] p-4">
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
        <TrackMap circuitId={circuitId} />
        <LapTimeChart />
      </div>
    </div>
  )
}
