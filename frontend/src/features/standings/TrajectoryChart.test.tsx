import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { TrajectoryChart, TrajectoryTooltip } from './TrajectoryChart'
import { sampleTrajectory } from '../../shared/mocks/handlers/ergastHandlers'

// Recharts renders SVG which jsdom doesn't support; mock the module,
// mirroring the established convention in live-race/LapTimeChart.test.tsx.
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

function renderChart() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <TrajectoryChart />
    </QueryClientProvider>,
  )
}

describe('TrajectoryChart', () => {
  it('renders one line per driver once loaded', async () => {
    renderChart()

    await waitFor(() => expect(screen.getByTestId('line-chart')).toBeInTheDocument())

    for (const trajectory of sampleTrajectory) {
      expect(screen.getByTestId(`line-${trajectory.driverId}`)).toBeInTheDocument()
    }
  })

  it('shows the card title', async () => {
    renderChart()
    expect(screen.getByText('Championship Trajectory')).toBeInTheDocument()
  })
})

describe('TrajectoryTooltip', () => {
  it('shows race name, result position, and points scored that round for the hovered point', () => {
    const norris = sampleTrajectory[0]

    render(
      <TrajectoryTooltip
        active
        label={1}
        payload={[{ dataKey: 'norris', value: 25, color: '#ff8a1e' }]}
        trajectories={sampleTrajectory}
      />,
    )

    expect(screen.getByText(norris.driverName)).toBeInTheDocument()
    expect(screen.getByText(norris.points[0].raceName)).toBeInTheDocument()
    expect(screen.getByText('P1')).toBeInTheDocument()
    expect(screen.getByText('+25')).toBeInTheDocument()
  })

  it('renders nothing when inactive', () => {
    const { container } = render(
      <TrajectoryTooltip
        active={false}
        label={1}
        payload={[{ dataKey: 'norris', value: 25, color: '#ff8a1e' }]}
        trajectories={sampleTrajectory}
      />,
    )

    expect(container).toBeEmptyDOMElement()
  })
})
