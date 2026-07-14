import { useRef, useState } from 'react'
import { toPng } from 'html-to-image'
import { useConstructorStandings, useDriverStandings } from '../../shared/api/ergast'
import { constructorBadgeLabel, constructorColor } from '../standings/constructorColors'
import { teamPrincipal } from '../../shared/data/teamPrincipals'
import { driverPhotoUrl } from '../../shared/data/driverPhotos'
import type { CompleteFanCardPicks } from './useFanCardStore'

interface FanCardProps {
  picks: CompleteFanCardPicks
}

function driverInitials(name: string): string {
  const parts = name.trim().split(/\s+/)
  const initials = parts.length === 1 ? parts[0].slice(0, 2) : parts[0][0] + parts[parts.length - 1][0]
  return initials.toUpperCase()
}

export function FanCard({ picks }: FanCardProps) {
  const cardRef = useRef<HTMLDivElement>(null)
  const [isExporting, setIsExporting] = useState(false)
  const [photoFailed, setPhotoFailed] = useState(false)

  const { data: drivers } = useDriverStandings()
  const { data: constructors } = useConstructorStandings()

  const driverStanding = drivers?.find((d) => d.driverId === picks.driverId)
  const constructorStanding = constructors?.find((c) => c.constructorName === picks.constructorName)
  const principal = teamPrincipal(picks.constructorName)
  const teamColor = constructorColor(picks.constructorName)

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
    <div className="inline-block rounded-lg border border-accent-editorial bg-bg-card p-4">
      <div
        ref={cardRef}
        data-testid="fan-card"
        className="flex aspect-[5/7] w-[226px] flex-col overflow-hidden rounded-lg border border-border-soft bg-bg-card"
      >
        <div data-testid="fan-card-team-rule" className="h-1 shrink-0" style={{ background: teamColor }} />

        <div className="relative flex flex-1 items-center justify-center overflow-hidden bg-bg-inset">
          {photoFailed ? (
            <span
              data-testid="fan-card-photo-fallback"
              className="flex h-16 w-16 items-center justify-center rounded-full bg-bg-card-hover text-[20px] font-bold text-text-secondary"
            >
              {driverInitials(picks.driverName)}
            </span>
          ) : (
            <img
              src={driverPhotoUrl(picks.driverId)}
              alt={picks.driverName}
              onError={() => setPhotoFailed(true)}
              className="h-full w-full object-cover"
            />
          )}
        </div>

        <div className="border-t border-border-soft px-3.5 py-3">
          <div className="mb-2 text-[15px] font-bold tracking-[-0.01em] text-text-primary">{picks.driverName}</div>

          <div className="mb-1.5 flex items-center gap-1.5">
            <span
              className="flex h-[18px] w-[18px] shrink-0 items-center justify-center rounded-full text-[8px] font-bold text-black"
              style={{ background: teamColor }}
            >
              {constructorBadgeLabel(picks.constructorName)}
            </span>
            <span className="text-[12px] font-semibold text-text-secondary">{picks.constructorName}</span>
          </div>

          {principal && (
            <div className="mb-2 text-[11px] text-text-tertiary">
              Team Principal: <b className="font-semibold text-text-secondary">{principal}</b>
            </div>
          )}

          <div className="mb-2 text-[11px] text-text-tertiary">
            {driverStanding && (
              <div>
                P{driverStanding.position} · {driverStanding.points} pts
              </div>
            )}
            {constructorStanding && (
              <div>
                {picks.constructorName} · P{constructorStanding.position} · {constructorStanding.points} pts
              </div>
            )}
            <div>{picks.circuitName}</div>
          </div>

          <div
            className="border-t border-dashed border-border-soft pt-2 text-[18px] leading-none text-text-primary"
            style={{ fontFamily: '"Segoe Script","Brush Script MT",cursive', transform: 'rotate(-1.5deg)' }}
          >
            {picks.driverName}
          </div>
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
      </div>
    </div>
  )
}
