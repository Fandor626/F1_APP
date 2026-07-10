import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it } from 'vitest'
import { CircuitProfilePage } from './CircuitProfilePage'
import { server } from '../../shared/test/server'
import { sampleCircuitProfile } from '../../shared/mocks/handlers/ergastHandlers'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

// CircuitTrackLayout fetches /circuit-configs/{circuitId}.json directly
// (same asset TrackMap.tsx uses) — not part of the Ergast API mock domain,
// so it needs its own MSW handler here rather than living in ergastHandlers.
beforeEach(() => {
  server.use(
    http.get(`${API_BASE_URL}/circuit-configs/monza.json`, () =>
      HttpResponse.json({ circuitId: 'monza', viewBox: '0 0 100 100', trackPath: 'M0,0 L100,100' }),
    ),
    http.get(`${API_BASE_URL}/circuit-configs/:circuitId.json`, () => new HttpResponse(null, { status: 404 })),
  )
})

function renderPage(circuitId: string) {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/circuits/${circuitId}`]}>
        <Routes>
          <Route path="/circuits/:circuitId" element={<CircuitProfilePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CircuitProfilePage', () => {
  it('renders lap record, past winners, and circuit stats for a known circuit', async () => {
    renderPage('monza')

    await waitFor(() => expect(screen.getByRole('heading', { name: sampleCircuitProfile!.circuitName })).toBeInTheDocument())

    expect(screen.getByText(sampleCircuitProfile!.lapRecord!.driverName)).toBeInTheDocument()
    expect(screen.getByRole('table')).toBeInTheDocument()
    for (const winner of sampleCircuitProfile!.pastWinners) {
      expect(screen.getByText(winner.driverName)).toBeInTheDocument()
    }
    expect(screen.getByText(/5\.793 km/)).toBeInTheDocument()
  })

  it('shows a not-found state for an unknown circuit', async () => {
    renderPage('not_a_circuit')

    await waitFor(() => expect(screen.getByText('Circuit not found.')).toBeInTheDocument())
  })

  it('shows a clear error message when the request fails', async () => {
    server.use(http.get(`${API_BASE_URL}/api/circuits/:circuitId`, () => HttpResponse.error()))

    renderPage('monza')

    await waitFor(() =>
      expect(screen.getByText("Couldn't reach the server — try refreshing.")).toBeInTheDocument(),
    )
  })

  it('shows a graceful message when circuit stats are unavailable', async () => {
    server.use(
      http.get(`${API_BASE_URL}/api/circuits/:circuitId`, () =>
        HttpResponse.json({ ...sampleCircuitProfile, stats: null, lapRecord: null }),
      ),
    )

    renderPage('monza')

    await waitFor(() => expect(screen.getByText('Circuit stats not available.')).toBeInTheDocument())
    expect(screen.getByText('No lap record data available.')).toBeInTheDocument()
  })
})
