import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { FanCard } from './FanCard'
import type { CompleteFanCardPicks } from './useFanCardStore'

const toPngMock = vi.fn().mockResolvedValue('data:image/png;base64,mock')
vi.mock('html-to-image', () => ({
  toPng: (...args: unknown[]) => toPngMock(...args),
}))

const picks: CompleteFanCardPicks = {
  driverId: 'norris',
  driverName: 'Norris',
  constructorName: 'McLaren',
  circuitId: 'bahrain',
  circuitName: 'Bahrain International Circuit',
}

function renderCard(overridePicks: CompleteFanCardPicks = picks) {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <FanCard picks={overridePicks} onEdit={vi.fn()} />
    </QueryClientProvider>,
  )
}

describe('FanCard', () => {
  it('renders the driver photo image pointed at the curated asset path', () => {
    renderCard()

    const img = screen.getByRole('img', { name: 'Norris' })
    expect(img).toHaveAttribute('src', '/fan-card-assets/drivers/norris.jpg')
  })

  it('falls back to an initials placeholder when the photo fails to load', () => {
    renderCard()

    const img = screen.getByRole('img', { name: 'Norris' })
    fireEvent.error(img)

    expect(screen.queryByRole('img', { name: 'Norris' })).not.toBeInTheDocument()
    expect(screen.getByTestId('fan-card-photo-fallback')).toHaveTextContent('NO')
  })

  it('shows the team principal when known', () => {
    renderCard()

    expect(screen.getByText('Team Principal:')).toBeInTheDocument()
    expect(screen.getByText('Andrea Stella')).toBeInTheDocument()
  })

  it('omits the team principal line when the constructor is unknown', () => {
    renderCard({ ...picks, constructorName: 'Some New Team' })

    expect(screen.queryByText('Team Principal:', { exact: false })).not.toBeInTheDocument()
  })

  it('renders the 4px team rule styled with the constructor color', () => {
    renderCard()

    const rule = screen.getByTestId('fan-card-team-rule')
    expect(rule).toHaveStyle({ background: 'var(--color-team-mclaren)' })
  })

  it('keeps driver, constructor, and circuit text content from the MVP card', () => {
    renderCard()

    const card = screen.getByTestId('fan-card')
    expect(card).toHaveTextContent('Norris')
    expect(card).toHaveTextContent('McLaren')
    expect(card).toHaveTextContent('Bahrain International Circuit')
  })

  it('still exports the card as a PNG when the download button is clicked', async () => {
    renderCard()

    fireEvent.click(screen.getByRole('button', { name: 'Download Image' }))

    await waitFor(() => expect(toPngMock).toHaveBeenCalledTimes(1))
  })
})
