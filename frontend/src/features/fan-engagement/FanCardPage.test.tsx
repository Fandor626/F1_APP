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
  useFanCardStore.setState({
    driverId: null,
    driverName: null,
    constructorName: null,
    circuitId: null,
    circuitName: null,
  })
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
  it('shows the setup wizard when no picks exist yet', () => {
    renderPage()

    expect(screen.getByText('Set Up Your Fan Card')).toBeInTheDocument()
    expect(screen.queryByTestId('fan-card')).not.toBeInTheDocument()
  })

  it('shows the fan card with current stats once picks are saved', async () => {
    renderPage()

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

  it('re-opens the wizard with existing picks retained when editing', async () => {
    renderPage()
    await fillWizardAndSave()
    await waitFor(() => expect(screen.getByTestId('fan-card')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Picks' }))

    expect(screen.getByText('Set Up Your Fan Card')).toBeInTheDocument()
    expect(screen.getByLabelText('Favourite Driver')).toHaveValue(sampleDriverStandings[0].driverId)
  })

  it('persists picks under the versioned localStorage key', async () => {
    renderPage()
    await fillWizardAndSave()
    await waitFor(() => expect(screen.getByTestId('fan-card')).toBeInTheDocument())

    const raw = window.localStorage.getItem('f1app__fanCard__v1')
    expect(raw).toBeTruthy()
    const parsed = JSON.parse(raw!)
    expect(parsed.state.driverId).toBe(sampleDriverStandings[0].driverId)
  })

  it('exports the card as a PNG when the download button is clicked', async () => {
    renderPage()
    await fillWizardAndSave()
    await waitFor(() => expect(screen.getByTestId('fan-card')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Download Image' }))

    await waitFor(() => expect(toPngMock).toHaveBeenCalledTimes(1))
  })
})
