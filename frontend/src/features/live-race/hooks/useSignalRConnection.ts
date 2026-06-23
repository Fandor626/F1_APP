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

  useEffect(() => {
    const handleSnapshot = (snapshot: RaceSnapshotMessage) => {
      setDrivers(normalizeSnapshot(snapshot.drivers))
      setLapChart(snapshot.lapChart)
      setLastSnapshotTime(new Date())
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

    return () => {
      raceHubConnection.off('RaceSnapshot', handleSnapshot)
    }
  }, [setConnectionStatus, setDrivers, setLapChart, setLastSnapshotTime])
}
