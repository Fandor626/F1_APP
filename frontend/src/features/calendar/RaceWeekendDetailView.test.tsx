import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { RaceWeekendDetailView } from './RaceWeekendDetailView'
import { server } from '../../shared/test/server'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

function renderDetail(round: number) {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/races/${round}`]}>
        <Routes>
          <Route path="/races/:round" element={<RaceWeekendDetailView />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('RaceWeekendDetailView', () => {
  it('lists a standard weekend\'s sessions in FP1/FP2/FP3/Qualifying/Race order', async () => {
    renderDetail(1)

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Bahrain Grand Prix' })).toBeInTheDocument())

    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(5)
    expect(items[0]).toHaveTextContent('FP1')
    expect(items[1]).toHaveTextContent('FP2')
    expect(items[2]).toHaveTextContent('FP3')
    expect(items[3]).toHaveTextContent('Qualifying')
    expect(items[4]).toHaveTextContent('Race')
  })

  it('shows Sprint Qualifying and Sprint in place of FP2/FP3 on a sprint weekend', async () => {
    renderDetail(2)

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Saudi Arabian Grand Prix' })).toBeInTheDocument(),
    )

    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(5)
    expect(items[0]).toHaveTextContent('FP1')
    expect(items[1]).toHaveTextContent('Sprint Qualifying')
    expect(items[2]).toHaveTextContent('Sprint')
    expect(items[3]).toHaveTextContent('Qualifying')
    expect(items[4]).toHaveTextContent('Race')
  })

  it('shows a clear error message when the race detail request fails', async () => {
    server.use(http.get(`${API_BASE_URL}/api/races/:round`, () => HttpResponse.error()))

    renderDetail(1)

    await waitFor(() =>
      expect(screen.getByText('Couldn\'t reach the server — try refreshing.')).toBeInTheDocument(),
    )
  })

  it('links back to the calendar', async () => {
    renderDetail(1)

    expect(screen.getByRole('link', { name: '← Calendar' })).toHaveAttribute('href', '/')
  })

  it('links the circuit name to its profile page', async () => {
    renderDetail(1)

    await waitFor(() =>
      expect(screen.getByRole('link', { name: 'Bahrain International Circuit' })).toHaveAttribute(
        'href',
        '/circuits/bahrain',
      ),
    )
  })
})
