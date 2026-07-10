import { useState } from 'react'
import { FanCard } from './FanCard'
import { FanCardWizard } from './FanCardWizard'
import { hasFanCardPicks, useFanCardStore } from './useFanCardStore'

export function FanCardPage() {
  const picks = useFanCardStore((s) => s)
  const [isEditing, setIsEditing] = useState(false)

  const showWizard = isEditing || !hasFanCardPicks(picks)

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">My F1 Fan Card</h1>
      <p className="mb-7 text-[13px] text-text-secondary">
        Pick your favourite driver, constructor, and circuit to build your fan card.
      </p>

      {showWizard ? (
        <FanCardWizard onDone={() => setIsEditing(false)} />
      ) : (
        hasFanCardPicks(picks) && <FanCard picks={picks} onEdit={() => setIsEditing(true)} />
      )}
    </div>
  )
}
