import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FanCardPromptModal, PROMPT_SUPPRESSION_MS } from './FanCardPromptModal'
import { useFanCardStore } from './useFanCardStore'
import { sampleConstructorStandings, sampleDriverStandings } from '../../shared/mocks/handlers/ergastHandlers'

const navigateMock = vi.fn()
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => navigateMock }
})

const EMPTY_PICKS = {
  driverId: null,
  driverName: null,
  constructorName: null,
  circuitId: null,
  circuitName: null,
}

const DISMISSED_AT_KEY = 'f1app__fanCardPromptDismissedAt__v1'

function renderModal() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <FanCardPromptModal />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  window.localStorage.clear()
  navigateMock.mockClear()
  useFanCardStore.setState(EMPTY_PICKS)
})

describe('FanCardPromptModal', () => {
  it('does not render when the user already has complete fan card picks', () => {
    useFanCardStore.setState({
      driverId: 'norris',
      driverName: 'Lando Norris',
      constructorName: 'McLaren',
      circuitId: 'bahrain',
      circuitName: 'Bahrain International Circuit',
    })

    renderModal()

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('renders when picks are empty and no suppression is active', () => {
    renderModal()

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Set up your Fan Card')).toBeInTheDocument()
  })

  it('does not render when a dismissal timestamp is within the suppression window', () => {
    window.localStorage.setItem(DISMISSED_AT_KEY, JSON.stringify(Date.now() - 1000))

    renderModal()

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('renders again once the suppression window has passed', () => {
    window.localStorage.setItem(DISMISSED_AT_KEY, JSON.stringify(Date.now() - PROMPT_SUPPRESSION_MS - 1000))

    renderModal()

    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('clicking "Not now" writes a dismissal timestamp and closes the modal', () => {
    renderModal()

    fireEvent.click(screen.getByRole('button', { name: 'Not now' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    const raw = window.localStorage.getItem(DISMISSED_AT_KEY)
    expect(raw).toBeTruthy()
    expect(Date.now() - (JSON.parse(raw!) as number)).toBeLessThan(1000)
  })

  it('clicking "Create Fan Card" swaps to the wizard fields', () => {
    renderModal()

    fireEvent.click(screen.getByRole('button', { name: 'Create Fan Card' }))

    expect(screen.getByText('Set Up Your Fan Card')).toBeInTheDocument()
    expect(screen.getByLabelText('Favourite Driver')).toBeInTheDocument()
    expect(screen.getByLabelText('Favourite Constructor')).toBeInTheDocument()
    expect(screen.getByLabelText('Favourite Circuit')).toBeInTheDocument()
  })

  it('completing the wizard closes the modal and navigates to /fan-card', async () => {
    renderModal()
    fireEvent.click(screen.getByRole('button', { name: 'Create Fan Card' }))

    const driver = sampleDriverStandings[0]
    const constructor = sampleConstructorStandings[0]

    await waitFor(() => expect(screen.getByRole('option', { name: driver.driverName })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Favourite Driver'), { target: { value: driver.driverId } })

    await waitFor(() => expect(screen.getByRole('option', { name: constructor.constructorName })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Favourite Constructor'), { target: { value: constructor.constructorName } })

    await waitFor(() => expect(screen.getByRole('option', { name: 'Bahrain International Circuit' })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Favourite Circuit'), { target: { value: 'bahrain' } })

    fireEvent.click(screen.getByRole('button', { name: 'Save Fan Card' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(navigateMock).toHaveBeenCalledWith('/fan-card')
    // AC 6/task note: completing the wizard must not write a suppression
    // timestamp — only "Not now" does.
    expect(window.localStorage.getItem(DISMISSED_AT_KEY)).toBeNull()
  })
})
