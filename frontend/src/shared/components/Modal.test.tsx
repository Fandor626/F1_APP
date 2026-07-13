import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { Modal } from './Modal'

function Content() {
  return (
    <div>
      <button type="button">First</button>
      <button type="button">Second</button>
      <button type="button">Third</button>
    </div>
  )
}

describe('Modal', () => {
  it('renders nothing when isOpen is false', () => {
    render(
      <Modal isOpen={false} onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('portal-renders as a descendant of document.body when open', () => {
    render(
      <Modal isOpen onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    const dialog = screen.getByRole('dialog')
    expect(document.body.contains(dialog)).toBe(true)
  })

  it('has role="dialog" and aria-modal="true"', () => {
    render(
      <Modal isOpen onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveAttribute('aria-modal', 'true')
    expect(dialog).toHaveAttribute('aria-label', 'Test modal')
  })

  it('pressing Escape calls onClose', () => {
    const onClose = vi.fn()
    render(
      <Modal isOpen onClose={onClose} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    fireEvent.keyDown(document, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('clicking the backdrop calls onClose', () => {
    const onClose = vi.fn()
    render(
      <Modal isOpen onClose={onClose} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    fireEvent.click(screen.getByTestId('modal-backdrop'))
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('clicking inside the content panel does not call onClose', () => {
    const onClose = vi.fn()
    render(
      <Modal isOpen onClose={onClose} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    fireEvent.click(screen.getByText('First'))
    expect(onClose).not.toHaveBeenCalled()
  })

  it('moves focus into the modal on open', () => {
    render(
      <Modal isOpen onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    expect(screen.getByText('First')).toHaveFocus()
  })

  it('Tab from the last focusable element wraps to the first', () => {
    render(
      <Modal isOpen onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    screen.getByText('Third').focus()
    fireEvent.keyDown(document, { key: 'Tab' })
    expect(screen.getByText('First')).toHaveFocus()
  })

  it('Shift+Tab from the first focusable element wraps to the last', () => {
    render(
      <Modal isOpen onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    screen.getByText('First').focus()
    fireEvent.keyDown(document, { key: 'Tab', shiftKey: true })
    expect(screen.getByText('Third')).toHaveFocus()
  })

  it('restores focus to the trigger element once closed', () => {
    const trigger = document.createElement('button')
    trigger.textContent = 'Open'
    document.body.appendChild(trigger)
    trigger.focus()
    expect(trigger).toHaveFocus()

    const { rerender } = render(
      <Modal isOpen onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )
    expect(trigger).not.toHaveFocus()

    rerender(
      <Modal isOpen={false} onClose={vi.fn()} ariaLabel="Test modal">
        <Content />
      </Modal>,
    )

    expect(trigger).toHaveFocus()
    document.body.removeChild(trigger)
  })
})
