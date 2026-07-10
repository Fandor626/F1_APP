import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { StreakCounter } from './StreakCounter'
import { STREAK_STORAGE_KEY } from './streakStorage'

describe('StreakCounter', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  it('renders nothing when no streak is recorded', () => {
    const { container } = render(<StreakCounter />)
    expect(container).toBeEmptyDOMElement()
  })

  it('renders the current streak count from localStorage', () => {
    window.localStorage.setItem(STREAK_STORAGE_KEY, JSON.stringify({ count: 3, lastCountedIndex: 5 }))

    render(<StreakCounter />)

    expect(screen.getByTestId('streak-counter')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
  })
})
