import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import { ChampionshipSidebar } from './ChampionshipSidebar'
import { sampleConstructorStandings, sampleDriverStandings } from '../../shared/mocks/handlers/ergastHandlers'

function renderSidebar() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <ChampionshipSidebar />
    </QueryClientProvider>,
  )
}

describe('ChampionshipSidebar', () => {
  it('is a labelled, independently reachable landmark region', async () => {
    renderSidebar()

    await waitFor(() => expect(screen.getByText(sampleDriverStandings[0].driverName)).toBeInTheDocument())

    expect(screen.getByRole('complementary', { name: 'Championship standings' })).toBeInTheDocument()
  })

  it('shows the top 3 drivers and top 3 constructors, sourced from the standings data', async () => {
    renderSidebar()

    await waitFor(() => expect(screen.getByText(sampleDriverStandings[0].driverName)).toBeInTheDocument())

    for (const standing of sampleDriverStandings.slice(0, 3)) {
      expect(screen.getByText(standing.driverName)).toBeInTheDocument()
    }
    expect(screen.queryByText(sampleDriverStandings[3].driverName)).not.toBeInTheDocument()

    for (const standing of sampleConstructorStandings.slice(0, 3)) {
      expect(screen.getByText(standing.constructorName)).toBeInTheDocument()
    }
  })

  it('toggles the mobile drawer content via the summary button', async () => {
    renderSidebar()

    await waitFor(() => expect(screen.getByText(sampleDriverStandings[0].driverName)).toBeInTheDocument())

    const toggle = screen.getByRole('button', { name: 'Championship standings' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')

    fireEvent.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'true')

    fireEvent.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
  })
})
