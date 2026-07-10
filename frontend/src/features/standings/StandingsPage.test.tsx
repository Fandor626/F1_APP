import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { StandingsPage } from './StandingsPage'
import { server } from '../../shared/test/server'
import { sampleConstructorStandings, sampleDriverStandings } from '../../shared/mocks/handlers/ergastHandlers'

// Recharts' ResponsiveContainer needs ResizeObserver, which jsdom doesn't
// provide; mock the module, mirroring live-race/LapTimeChart.test.tsx.
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: ({ dataKey }: { dataKey: string }) => <div data-testid={`line-${dataKey}`} />,
  XAxis: () => null,
  YAxis: () => null,
  Tooltip: () => null,
  CartesianGrid: () => null,
}))

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <StandingsPage />
    </QueryClientProvider>,
  )
}

describe('StandingsPage', () => {
  it('renders every driver in a real table by default', async () => {
    renderPage()

    await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument())

    const table = within(screen.getByRole('table'))
    for (const standing of sampleDriverStandings) {
      expect(table.getByText(standing.driverName)).toBeInTheDocument()
    }
  })

  it('switches to constructors instantly with no reload', async () => {
    renderPage()

    await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument())
    expect(within(screen.getByRole('table')).getByText('Norris')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Constructors' }))

    expect(screen.queryByText('Norris')).not.toBeInTheDocument()
    await waitFor(() => {
      const table = within(screen.getByRole('table'))
      for (const standing of sampleConstructorStandings) {
        expect(table.getByText(standing.constructorName)).toBeInTheDocument()
      }
    })
  })

  it('shows a clear error message when the standings request fails', async () => {
    server.use(http.get(`${API_BASE_URL}/api/standings/drivers`, () => HttpResponse.error()))

    renderPage()

    await waitFor(() =>
      expect(screen.getByText("Couldn't reach the server — try refreshing.")).toBeInTheDocument(),
    )
  })
})
