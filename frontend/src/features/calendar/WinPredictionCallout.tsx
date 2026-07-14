import { useState } from 'react'
import type { WinProbabilityEntry } from '../../shared/api/ergast'
import { WinProbabilityWidget } from './WinProbabilityWidget'

interface WinPredictionCalloutProps {
  entries: WinProbabilityEntry[]
}

// The win-probability endpoint only exposes grid position and a
// championship-weighted probability — no pole-history/practice-pace data —
// so the "concrete reason" is grounded in those two fields only.
function buildReason(top: WinProbabilityEntry, second: WinProbabilityEntry | undefined): string {
  if (top.gridPosition === 1) {
    return `${top.driverName} starts from pole position, giving ${top.constructorName} the strongest platform to convert into a win.`
  }
  if (second && top.winProbability - second.winProbability >= 10) {
    return `${top.driverName} qualified P${top.gridPosition}, but our model still gives them a clear edge over the rest of the field.`
  }
  return `${top.driverName} qualified P${top.gridPosition}, putting them in the strongest position on a tightly-matched grid.`
}

// AD-13's Modal aside, this is the app's first inline expand/collapse —
// modeled on ChampionshipSidebar.tsx's button + aria-expanded/aria-controls
// pattern rather than the Modal primitive (this reveals inline content, not
// an overlay).
export function WinPredictionCallout({ entries }: WinPredictionCalloutProps) {
  const [isExpanded, setIsExpanded] = useState(false)

  if (entries.length === 0) return null

  const sorted = [...entries].sort((a, b) => b.winProbability - a.winProbability)
  const [top, second] = sorted

  return (
    <div
      className="mt-8 rounded-lg border border-accent-editorial bg-bg-card px-[22px] py-5"
      data-testid="win-prediction-callout"
    >
      <div className="mb-2.5 text-[11px] font-semibold tracking-[0.06em] text-accent-editorial uppercase">
        Win Prediction
      </div>
      <p className="mb-3.5 text-[15px] leading-relaxed text-text-primary">
        Most likely to win: <span className="font-bold text-accent-editorial">{top.driverName}</span>.{' '}
        {buildReason(top, second)}
      </p>
      <button
        type="button"
        onClick={() => setIsExpanded((expanded) => !expanded)}
        aria-expanded={isExpanded}
        aria-controls="win-prediction-raw-table"
        className="rounded-md border border-border-soft px-3 py-1.5 text-[11.5px] font-semibold text-text-secondary hover:border-accent-editorial hover:text-accent-editorial"
      >
        See grid-by-grid win probability {isExpanded ? '▴' : '▾'}
      </button>

      {isExpanded && (
        <div id="win-prediction-raw-table" className="border-t border-dashed border-border-soft pt-2">
          <WinProbabilityWidget entries={entries} />
        </div>
      )}
    </div>
  )
}
