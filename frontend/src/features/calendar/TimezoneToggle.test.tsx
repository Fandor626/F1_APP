import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { TimezoneToggle } from './TimezoneToggle'

describe('TimezoneToggle', () => {
  it('marks Local as active when mode is local', () => {
    render(<TimezoneToggle mode="local" onToggle={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Local' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Track' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('marks Track as active when mode is track', () => {
    render(<TimezoneToggle mode="track" onToggle={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Track' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Local' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('calls onToggle when Track is clicked from local mode', () => {
    const onToggle = vi.fn()
    render(<TimezoneToggle mode="local" onToggle={onToggle} />)

    fireEvent.click(screen.getByRole('button', { name: 'Track' }))

    expect(onToggle).toHaveBeenCalledTimes(1)
  })

  it('calls onToggle when Local is clicked from track mode', () => {
    const onToggle = vi.fn()
    render(<TimezoneToggle mode="track" onToggle={onToggle} />)

    fireEvent.click(screen.getByRole('button', { name: 'Local' }))

    expect(onToggle).toHaveBeenCalledTimes(1)
  })
})
