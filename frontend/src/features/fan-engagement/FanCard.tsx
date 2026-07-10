import { useRef, useState } from 'react'
import { toPng } from 'html-to-image'
import { useConstructorStandings, useDriverStandings } from '../../shared/api/ergast'
import type { CompleteFanCardPicks } from './useFanCardStore'

interface FanCardProps {
  picks: CompleteFanCardPicks
  onEdit: () => void
}

export function FanCard({ picks, onEdit }: FanCardProps) {
  const cardRef = useRef<HTMLDivElement>(null)
  const [isExporting, setIsExporting] = useState(false)

  const { data: drivers } = useDriverStandings()
  const { data: constructors } = useConstructorStandings()

  const driverStanding = drivers?.find((d) => d.driverId === picks.driverId)
  const constructorStanding = constructors?.find((c) => c.constructorName === picks.constructorName)

  async function handleExport() {
    if (!cardRef.current) return
    setIsExporting(true)
    try {
      const dataUrl = await toPng(cardRef.current)
      const link = document.createElement('a')
      link.download = 'f1-fan-card.png'
      link.href = dataUrl
      link.click()
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <div className="rounded-lg border border-accent-editorial bg-bg-card px-[22px] py-[18px]">
      <div ref={cardRef} className="bg-bg-card px-1 py-1" data-testid="fan-card">
        <div className="mb-1 text-[15px] font-bold tracking-[-0.01em] text-text-primary">My F1 Fan Card</div>
        <p className="mb-3 text-[12px] text-text-tertiary">Current season</p>

        <div className="border-b border-bg-card-hover py-3">
          <div className="text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Favourite Driver
          </div>
          <div className="mt-1 text-[15px] font-bold text-text-primary">{picks.driverName}</div>
          {driverStanding && (
            <div className="text-[12.5px] text-text-secondary">
              P{driverStanding.position} · {driverStanding.points} pts
            </div>
          )}
        </div>

        <div className="border-b border-bg-card-hover py-3">
          <div className="text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Favourite Constructor
          </div>
          <div className="mt-1 text-[15px] font-bold text-text-primary">{picks.constructorName}</div>
          {constructorStanding && (
            <div className="text-[12.5px] text-text-secondary">
              P{constructorStanding.position} · {constructorStanding.points} pts
            </div>
          )}
        </div>

        <div className="py-3 last:border-b-0">
          <div className="text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">
            Favourite Circuit
          </div>
          <div className="mt-1 text-[15px] font-bold text-text-primary">{picks.circuitName}</div>
        </div>
      </div>

      <div className="mt-4 flex gap-3">
        <button
          type="button"
          onClick={handleExport}
          disabled={isExporting}
          className="rounded-md border border-border-soft px-4 py-2 text-[13px] font-semibold text-text-primary hover:border-accent-editorial hover:text-accent-editorial disabled:opacity-50"
        >
          {isExporting ? 'Exporting…' : 'Download Image'}
        </button>
        <button
          type="button"
          onClick={onEdit}
          className="rounded-md border border-border-soft px-4 py-2 text-[13px] font-semibold text-text-secondary hover:border-accent-editorial hover:text-accent-editorial"
        >
          Edit Picks
        </button>
      </div>
    </div>
  )
}
