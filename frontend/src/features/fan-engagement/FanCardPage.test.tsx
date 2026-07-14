import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FanCardPage } from './FanCardPage'
import { useFanCardStore } from './useFanCardStore'
import { sampleConstructorStandings, sampleDriverStandings } from '../../shared/mocks/handlers/ergastHandlers'

const toPngMock = vi.fn().mockResolvedValue('data:image/png;base64,mock')
vi.mock('html-to-image', () => ({
  toPng: (...args: unknown[]) => toPngMock(...args),
}))

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <FanCardPage />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  window.localStorage.clear()
  useFanCardStore.setState({ cards: [] })
})

async function fillWizardAndSave() {
  const driver = sampleDriverStandings[0]
  const constructor = sampleConstructorStandings[0]

  // Each select's options only appear once its own query resolves — wait for
  // each one specifically before changing it, rather than assuming all three
  // independent queries settle by the time the (always-rendered) label shows.
  await waitFor(() => expect(screen.getByRole('option', { name: driver.driverName })).toBeInTheDocument())
  fireEvent.change(screen.getByLabelText('Favourite Driver'), { target: { value: driver.driverId } })

  await waitFor(() => expect(screen.getByRole('option', { name: constructor.constructorName })).toBeInTheDocument())
  fireEvent.change(screen.getByLabelText('Favourite Constructor'), { target: { value: constructor.constructorName } })

  await waitFor(() => expect(screen.getByRole('option', { name: 'Bahrain International Circuit' })).toBeInTheDocument())
  fireEvent.change(screen.getByLabelText('Favourite Circuit'), { target: { value: 'bahrain' } })

  fireEvent.click(screen.getByRole('button', { name: 'Save Fan Card' }))

  return { driver, constructor }
}

describe('FanCardPage', () => {
  it('shows only the "Add new card" tile as an empty state when there are zero cards', () => {
    renderPage()

    expect(screen.getByTestId('fan-card-add-tile')).toBeInTheDocument()
    expect(screen.queryByTestId('fan-card')).not.toBeInTheDocument()
  })

  it('opens the wizard when the "Add new card" tile is clicked', () => {
    renderPage()

    fireEvent.click(screen.getByTestId('fan-card-add-tile'))

    expect(screen.getByText('Set Up Your Fan Card')).toBeInTheDocument()
  })

  it('shows the fan card with current stats once picks are saved', async () => {
    renderPage()
    fireEvent.click(screen.getByTestId('fan-card-add-tile'))

    const { driver, constructor } = await fillWizardAndSave()

    await waitFor(() => expect(screen.getByTestId('fan-card')).toBeInTheDocument())

    const card = screen.getByTestId('fan-card')
    expect(card).toHaveTextContent(driver.driverName)
    expect(card).toHaveTextContent(`P${driver.position}`)
    expect(card).toHaveTextContent(`${driver.points} pts`)
    expect(card).toHaveTextContent(constructor.constructorName)
    expect(card).toHaveTextContent(`P${constructor.position}`)
    expect(card).toHaveTextContent('Bahrain International Circuit')
  })

  it('adds a second card alongside the first without overwriting it', async () => {
    renderPage()
    fireEvent.click(screen.getByTestId('fan-card-add-tile'))
    await fillWizardAndSave()
    await waitFor(() => expect(screen.getAllByTestId('fan-card')).toHaveLength(1))

    fireEvent.click(screen.getByTestId('fan-card-add-tile'))
    const driver = sampleDriverStandings[1]
    const constructor = sampleConstructorStandings[1]
    await waitFor(() => expect(screen.getByRole('option', { name: driver.driverName })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Favourite Driver'), { target: { value: driver.driverId } })
    await waitFor(() => expect(screen.getByRole('option', { name: constructor.constructorName })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Favourite Constructor'), { target: { value: constructor.constructorName } })
    await waitFor(() => expect(screen.getByRole('option', { name: 'Jeddah Corniche Circuit' })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Favourite Circuit'), { target: { value: 'jeddah' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save Fan Card' }))

    await waitFor(() => expect(screen.getAllByTestId('fan-card')).toHaveLength(2))
    expect(useFanCardStore.getState().cards).toHaveLength(2)
    expect(useFanCardStore.getState().cards[0].driverId).toBe(sampleDriverStandings[0].driverId)
    expect(useFanCardStore.getState().cards[1].driverId).toBe(driver.driverId)
  })

  it('persists cards under the versioned localStorage key', async () => {
    renderPage()
    fireEvent.click(screen.getByTestId('fan-card-add-tile'))
    await fillWizardAndSave()
    await waitFor(() => expect(screen.getByTestId('fan-card')).toBeInTheDocument())

    const raw = window.localStorage.getItem('f1app__fanCard__v1')
    expect(raw).toBeTruthy()
    const parsed = JSON.parse(raw!)
    expect(parsed.state.cards[0].driverId).toBe(sampleDriverStandings[0].driverId)
  })

  it('exports the card as a PNG when the download button is clicked', async () => {
    renderPage()
    fireEvent.click(screen.getByTestId('fan-card-add-tile'))
    await fillWizardAndSave()
    await waitFor(() => expect(screen.getByTestId('fan-card')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Download Image' }))

    await waitFor(() => expect(toPngMock).toHaveBeenCalledTimes(1))
  })
})
