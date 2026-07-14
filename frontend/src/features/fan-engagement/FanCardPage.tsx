import { useState } from 'react'
import { FanCard } from './FanCard'
import { FanCardWizard } from './FanCardWizard'
import { useFanCardStore } from './useFanCardStore'

export function FanCardPage() {
  const cards = useFanCardStore((s) => s.cards)
  const [isAdding, setIsAdding] = useState(false)

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">My F1 Fan Card</h1>
      <p className="mb-7 text-[13px] text-text-secondary">
        Pick your favourite driver, constructor, and circuit to build your fan card.
      </p>

      {isAdding ? (
        <FanCardWizard onDone={() => setIsAdding(false)} />
      ) : (
        <div
          data-testid="fan-card-grid"
          className="grid max-w-[920px] gap-[22px]"
          style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(226px, 1fr))' }}
        >
          {cards.map((card) => (
            <FanCard key={card.id} picks={card} />
          ))}
          <button
            type="button"
            onClick={() => setIsAdding(true)}
            data-testid="fan-card-add-tile"
            className="flex aspect-[5/7] w-[226px] items-center justify-center rounded-lg border border-dashed border-border-soft text-[13px] font-semibold text-text-secondary hover:border-accent-editorial hover:text-accent-editorial"
          >
            + Add new card
          </button>
        </div>
      )}
    </div>
  )
}
