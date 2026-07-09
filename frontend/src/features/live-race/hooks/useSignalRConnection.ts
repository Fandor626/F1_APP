import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import { raceHubConnection } from '../signalRClient'
import { useLiveRaceStore } from '../store/liveRaceStore'
import { normalizeSnapshot } from '../../../shared/utils/normalizeSnapshot'
import type { RaceSnapshotMessage } from '../../../shared/types/signalR'

// SignalR's onreconnecting/onreconnected/onclose have no corresponding `off`.
// Guard registration to the module lifetime so they don't accumulate across
// React Strict Mode double-invocations or future remounts.
let lifecycleHandlersAttached = false

export function useSignalRConnection() {
  const setConnectionStatus = useLiveRaceStore(s => s.setConnectionStatus)
  const setDrivers = useLiveRaceStore(s => s.setDrivers)
  const setLapChart = useLiveRaceStore(s => s.setLapChart)
  const setLastSnapshotTime = useLiveRaceStore(s => s.setLastSnapshotTime)
  const setSessionMode = useLiveRaceStore(s => s.setSessionMode)
  const setFallbackRaceName = useLiveRaceStore(s => s.setFallbackRaceName)
  const setCircuitId = useLiveRaceStore(s => s.setCircuitId)

  useEffect(() => {
    const handleSnapshot = (snapshot: RaceSnapshotMessage) => {
      setDrivers(normalizeSnapshot(snapshot.drivers))
      setLapChart(snapshot.lapChart)
      setLastSnapshotTime(new Date())
      setSessionMode(snapshot.sessionMode ?? 'live')
      if (snapshot.fallbackRaceName) setFallbackRaceName(snapshot.fallbackRaceName)
      if (snapshot.circuitId) setCircuitId(snapshot.circuitId)
    }

    raceHubConnection.on('RaceSnapshot', handleSnapshot)

    if (!lifecycleHandlersAttached) {
      raceHubConnection.onreconnecting(() => setConnectionStatus('reconnecting'))
      raceHubConnection.onreconnected(() => setConnectionStatus('connected'))
      raceHubConnection.onclose(() => setConnectionStatus('disconnected'))
      lifecycleHandlersAttached = true
    }

    if (raceHubConnection.state === signalR.HubConnectionState.Disconnected) {
      raceHubConnection
        .start()
        .then(() => setConnectionStatus('connected'))
        .catch(() => setConnectionStatus('disconnected'))
    }

    // Client-side fallback trigger — if 10s pass with no snapshot received after
    // connecting (server is running but no race weekend active, so it doesn't emit),
    // switch to fallback mode so LiveRacePage fetches from the REST endpoint.
    const noDataTimeout = window.setTimeout(() => {
      if (useLiveRaceStore.getState().lastSnapshotTime === null) {
        setSessionMode('fallback')
      }
    }, 10_000)

    return () => {
      raceHubConnection.off('RaceSnapshot', handleSnapshot)
      window.clearTimeout(noDataTimeout)
    }
  }, [setConnectionStatus, setDrivers, setLapChart, setLastSnapshotTime, setSessionMode, setFallbackRaceName, setCircuitId])
}
