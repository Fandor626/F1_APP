import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { DriverProfilePage } from './DriverProfilePage'
import { server } from '../../shared/test/server'
import { sampleDriverProfile } from '../../shared/mocks/handlers/ergastHandlers'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

// Recharts' ResponsiveContainer needs ResizeObserver, which jsdom doesn't
// provide; mock the module, mirroring standings/TrajectoryChart.test.tsx.
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: () => <div data-testid="career-line" />,
  XAxis: () => null,
  YAxis: () => null,
  Tooltip: () => null,
  CartesianGrid: () => null,
}))

function renderPage(driverId: string) {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/drivers/${driverId}`]}>
        <Routes>
          <Route path="/drivers/:driverId" element={<DriverProfilePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('DriverProfilePage', () => {
  it('renders career totals, constructor history, and the career chart for a known driver', async () => {
    renderPage('max_verstappen')

    await waitFor(() => expect(screen.getByRole('heading', { name: sampleDriverProfile!.fullName })).toBeInTheDocument())

    expect(screen.getByText(sampleDriverProfile!.careerTotals.wins)).toBeInTheDocument()
    expect(screen.getByText(sampleDriverProfile!.careerTotals.titles)).toBeInTheDocument()
    expect(screen.getByRole('table')).toBeInTheDocument()
    for (const entry of sampleDriverProfile!.constructorHistory) {
      expect(screen.getByText(entry.constructorNames.join(' / '))).toBeInTheDocument()
    }
    expect(screen.getByTestId('line-chart')).toBeInTheDocument()
  })

  it('shows a not-found state for an unknown driver', async () => {
    renderPage('not_a_driver')

    await waitFor(() => expect(screen.getByText('Driver not found.')).toBeInTheDocument())
  })

  it('shows a clear error message when the request fails', async () => {
    server.use(http.get(`${API_BASE_URL}/api/drivers/:driverId`, () => HttpResponse.error()))

    renderPage('max_verstappen')

    await waitFor(() =>
      expect(screen.getByText("Couldn't reach the server — try refreshing.")).toBeInTheDocument(),
    )
  })
})
