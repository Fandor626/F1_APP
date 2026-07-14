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

  it('renders a larger track layout panel using the same circuit-configs asset as the calendar card', async () => {
    server.use(
      http.get('/circuit-configs/bahrain.json', () =>
        HttpResponse.json({ circuitId: 'bahrain', viewBox: '0 0 500 500', trackPath: 'M10,10 L90,90 Z' }),
      ),
    )

    renderDetail(1)

    await waitFor(() =>
      expect(
        screen.getByRole('img', { name: 'Track layout: Bahrain International Circuit' }),
      ).toBeInTheDocument(),
    )
  })

  it('omits the track layout gracefully (no broken-image state) when the asset is unavailable', async () => {
    server.use(http.get('/circuit-configs/bahrain.json', () => new HttpResponse(null, { status: 404 })))

    renderDetail(1)

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Bahrain Grand Prix' })).toBeInTheDocument())
    expect(screen.getByTestId('race-weekend-track-layout')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('img', { name: /Track layout/ })).not.toBeInTheDocument())
  })

  it('shows the Track Records section with both lap records linking to driver profiles', async () => {
    renderDetail(1)

    await waitFor(() => expect(screen.getByText('Track Records')).toBeInTheDocument())
    expect(screen.getByRole('link', { name: 'Lewis Hamilton' })).toHaveAttribute('href', '/drivers/hamilton')
    expect(screen.getByRole('link', { name: 'Lando Norris' })).toHaveAttribute('href', '/drivers/norris')
    expect(screen.getByText('1:31.447')).toBeInTheDocument()
    expect(screen.getByText('1:32.608')).toBeInTheDocument()
  })

  it('omits the Track Records section rather than an error when a weekend has no lap record data', async () => {
    renderDetail(2)

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Saudi Arabian Grand Prix' })).toBeInTheDocument())
    expect(screen.queryByText('Track Records')).not.toBeInTheDocument()
  })
})
