import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { SeasonWrapped } from './SeasonWrapped'
import { server } from '../../../shared/test/server'
import { sampleSeasonWrapped } from '../../../shared/mocks/handlers/ergastHandlers'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

const toPngMock = vi.fn().mockResolvedValue('data:image/png;base64,mock')
vi.mock('html-to-image', () => ({
  toPng: (...args: unknown[]) => toPngMock(...args),
}))

function renderComponent() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <SeasonWrapped />
    </QueryClientProvider>,
  )
}

describe('SeasonWrapped', () => {
  it('renders nothing when the season is still in progress', async () => {
    server.use(http.get(`${API_BASE_URL}/api/standings/season-wrapped`, () => HttpResponse.json(null)))

    const { container } = renderComponent()

    await waitFor(() => expect(container).toBeEmptyDOMElement())
  })

  it('renders all five stats when the season is complete', async () => {
    renderComponent()

    await waitFor(() => expect(screen.getByText('Season Wrapped')).toBeInTheDocument())

    expect(screen.getByText(sampleSeasonWrapped!.mostDramaticRace.raceName)).toBeInTheDocument()
    expect(screen.getByText(sampleSeasonWrapped!.mostDnfs.driverName)).toBeInTheDocument()
    expect(screen.getByText(sampleSeasonWrapped!.biggestPointsComeback.driverName)).toBeInTheDocument()
    expect(screen.getByText(sampleSeasonWrapped!.mostPositionsGainedInARace.driverName)).toBeInTheDocument()
    expect(screen.getByText(sampleSeasonWrapped!.mostImprovedConstructor.constructorName)).toBeInTheDocument()
  })

  it('exports the card as a PNG when the download button is clicked', async () => {
    renderComponent()

    await waitFor(() => expect(screen.getByText('Season Wrapped')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Download Image' }))

    await waitFor(() => expect(toPngMock).toHaveBeenCalledTimes(1))
  })
})
