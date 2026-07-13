import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Modal } from '../../shared/components/Modal'
import { useLocalStorage } from '../../shared/hooks/useLocalStorage'
import { FanCardWizard } from './FanCardWizard'
import { hasFanCardPicks, useFanCardStore } from './useFanCardStore'

// Not pinned by any Acceptance Criterion — a dev-time constant (Architecture
// AD-13 deferred-item note). Easily changed later with no structural impact.
export const PROMPT_SUPPRESSION_MS = 7 * 24 * 60 * 60 * 1000

const DISMISSED_AT_KEY = 'f1app__fanCardPromptDismissedAt__v1'

// Self-governing: reads its own visibility inputs (fan card picks, dismissal
// timestamp) and renders nothing itself when not applicable — StandingsPage
// just mounts this with no props.
export function FanCardPromptModal() {
  const navigate = useNavigate()
  const picks = useFanCardStore((s) => s)
  const [dismissedAt, setDismissedAt] = useLocalStorage<number | null>(DISMISSED_AT_KEY, null)
  const [mode, setMode] = useState<'prompt' | 'wizard'>('prompt')

  // Date.now() is impure and can't be called directly during render (React
  // purity rules). Captured once per mount instead, the same sanctioned
  // shape as `useState(() => Math.random())` — this component doesn't need
  // to notice time passing while it stays mounted, only a fresh read each
  // time StandingsPage (and this modal with it) mounts.
  const [mountedAt] = useState(() => Date.now())
  const suppressed = dismissedAt !== null && mountedAt - dismissedAt < PROMPT_SUPPRESSION_MS
  const isOpen = !hasFanCardPicks(picks) && !suppressed

  // Shared by "Not now", Escape, and backdrop click — Modal exposes one
  // onClose, so every way of closing counts as a dismissal (AC 6). Resets
  // mode so a later reappearance (suppression window elapsed) starts back
  // at the prompt screen, not stranded on the wizard.
  function handleDismiss() {
    setDismissedAt(Date.now())
    setMode('prompt')
  }

  // No dismissal timestamp is written here — completing the wizard persists
  // picks via FanCardWizard's own onDone→handleSave, which flips
  // hasFanCardPicks to true and makes `isOpen` false on the next render
  // (AC 4 already covers this), closing the modal without extra state here.
  function handleWizardDone() {
    navigate('/fan-card')
  }

  return (
    <Modal isOpen={isOpen} onClose={handleDismiss} ariaLabel="Create your Fan Card">
      {mode === 'prompt' ? (
        <div className="rounded-lg border border-border-soft bg-bg-card px-[22px] py-[18px]">
          <h2 className="mb-2 text-[15px] font-bold tracking-[-0.01em] text-text-primary">
            Set up your Fan Card
          </h2>
          <p className="mb-5 text-[13px] text-text-secondary">
            Pick a favourite driver, constructor, and circuit — takes a few seconds.
          </p>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => setMode('wizard')}
              className="rounded-md border border-accent-editorial bg-accent-editorial px-4 py-2 text-[13px] font-semibold text-bg-app"
            >
              Create Fan Card
            </button>
            <button
              type="button"
              onClick={handleDismiss}
              className="rounded-md border border-border-soft px-4 py-2 text-[13px] font-semibold text-text-secondary hover:text-text-primary"
            >
              Not now
            </button>
          </div>
        </div>
      ) : (
        <FanCardWizard onDone={handleWizardDone} />
      )}
    </Modal>
  )
}
