import { GapList } from './GapList/GapList'
import { useSignalRConnection } from './hooks/useSignalRConnection'

export function LiveRacePage() {
  useSignalRConnection()

  return (
    <div className="min-h-screen bg-[#14171c] text-[#eef0f3] p-4">
      <h1 className="text-[26px] font-bold tracking-[-0.01em] mb-4">Live Race</h1>
      <GapList />
    </div>
  )
}
