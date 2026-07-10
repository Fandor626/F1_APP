import { useRef, useState } from 'react'
import { toPng } from 'html-to-image'
import type { SeasonWrapped } from '../../../shared/api/ergast'

interface StatRowProps {
  label: string
  headline: string
  detail: string
}

function StatRow({ label, headline, detail }: StatRowProps) {
  return (
    <div className="border-b border-bg-card-hover py-3 last:border-b-0">
      <div className="text-[11px] font-semibold tracking-[0.04em] text-text-tertiary uppercase">{label}</div>
      <div className="mt-1 text-[15px] font-bold text-text-primary">{headline}</div>
      <div className="text-[12.5px] text-text-secondary">{detail}</div>
    </div>
  )
}

interface SeasonWrappedCardProps {
  wrapped: NonNullable<SeasonWrapped>
}

export function SeasonWrappedCard({ wrapped }: SeasonWrappedCardProps) {
  const cardRef = useRef<HTMLDivElement>(null)
  const [isExporting, setIsExporting] = useState(false)

  async function handleExport() {
    if (!cardRef.current) return
    setIsExporting(true)
    try {
      const dataUrl = await toPng(cardRef.current)
      const link = document.createElement('a')
      link.download = 'f1-season-wrapped.png'
      link.href = dataUrl
      link.click()
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <div className="mt-6 rounded-lg border border-accent-editorial bg-bg-card px-[22px] py-[18px]">
      <div ref={cardRef} className="bg-bg-card px-1 py-1" data-testid="season-wrapped-card">
        <div className="mb-1 text-[15px] font-bold tracking-[-0.01em] text-text-primary">Season Wrapped</div>
        <p className="mb-3 text-[12px] text-text-tertiary">The season's defining moments</p>

        <StatRow
          label="Most Dramatic Race"
          headline={wrapped.mostDramaticRace.raceName}
          detail={`${wrapped.mostDramaticRace.totalPositionSwing} total position swing`}
        />
        <StatRow
          label="Most DNFs"
          headline={wrapped.mostDnfs.driverName}
          detail={`${wrapped.mostDnfs.value} retirement${wrapped.mostDnfs.value === 1 ? '' : 's'} · ${wrapped.mostDnfs.constructorName}`}
        />
        <StatRow
          label="Biggest Points Comeback"
          headline={wrapped.biggestPointsComeback.driverName}
          detail={`Closed a ${wrapped.biggestPointsComeback.value}-point gap to the leader · ${wrapped.biggestPointsComeback.constructorName}`}
        />
        <StatRow
          label="Most Positions Gained in a Race"
          headline={wrapped.mostPositionsGainedInARace.driverName}
          detail={`+${wrapped.mostPositionsGainedInARace.positionsGained} places at the ${wrapped.mostPositionsGainedInARace.raceName}`}
        />
        <StatRow
          label="Most-Improved Constructor"
          headline={wrapped.mostImprovedConstructor.constructorName}
          detail={`P${wrapped.mostImprovedConstructor.earlySeasonPosition} → P${wrapped.mostImprovedConstructor.finalPosition} (+${wrapped.mostImprovedConstructor.positionsImproved})`}
        />
      </div>

      <button
        type="button"
        onClick={handleExport}
        disabled={isExporting}
        className="mt-4 rounded-md border border-border-soft px-4 py-2 text-[13px] font-semibold text-text-primary hover:border-accent-editorial hover:text-accent-editorial disabled:opacity-50"
      >
        {isExporting ? 'Exporting…' : 'Download Image'}
      </button>
    </div>
  )
}
