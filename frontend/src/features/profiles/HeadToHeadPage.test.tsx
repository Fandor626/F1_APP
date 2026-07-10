import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { describe, expect, it } from 'vitest'
import { HeadToHeadPage } from './HeadToHeadPage'
import { server } from '../../shared/test/server'
import { sampleHeadToHeadComparison } from '../../shared/mocks/handlers/ergastHandlers'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <HeadToHeadPage />
    </QueryClientProvider>,
  )
}

async function selectDriver(label: string, query: string, optionName: string) {
  const combobox = screen.getByLabelText(label)
  fireEvent.change(combobox, { target: { value: query } })
  const option = await screen.findByRole('button', { name: optionName })
  fireEvent.click(option)
}

describe('HeadToHeadPage', () => {
  it('shows a prompt until two drivers are selected', () => {
    renderPage()

    expect(screen.getByText('Select two drivers to see their stats side by side.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('renders a side-by-side stat table once two drivers are selected', async () => {
    renderPage()

    await selectDriver('Driver A', 'Max', 'Max Verstappen')
    await selectDriver('Driver B', 'Lewis', 'Lewis Hamilton')

    await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument())

    const table = within(screen.getByRole('table'))
    expect(table.getByText('Max Verstappen')).toBeInTheDocument()
    expect(table.getByText('Lewis Hamilton')).toBeInTheDocument()
    expect(table.getByText('Qualifying Avg Position')).toBeInTheDocument()
    expect(table.getByText('Race Finish Avg')).toBeInTheDocument()
    expect(table.getByText('DNFs')).toBeInTheDocument()
    expect(table.getByText('Points Scored')).toBeInTheDocument()
    expect(table.getByText('Fastest Laps')).toBeInTheDocument()
    expect(table.getByText('Wins')).toBeInTheDocument()
    expect(table.getByText(String(sampleHeadToHeadComparison!.driverA.wins))).toBeInTheDocument()
  })

  it('recalculates when a season filter is applied', async () => {
    server.use(
      http.get(`${API_BASE_URL}/api/drivers/compare`, ({ request }) => {
        const url = new URL(request.url)
        if (url.searchParams.get('season') === '2023') {
          return HttpResponse.json({
            ...sampleHeadToHeadComparison,
            driverA: { ...sampleHeadToHeadComparison!.driverA, wins: 19 },
          })
        }
        return HttpResponse.json(sampleHeadToHeadComparison)
      }),
    )

    renderPage()

    await selectDriver('Driver A', 'Max', 'Max Verstappen')
    await selectDriver('Driver B', 'Lewis', 'Lewis Hamilton')

    await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Season (optional)'), { target: { value: '2023' } })

    await waitFor(() => expect(within(screen.getByRole('table')).getByText('19')).toBeInTheDocument())
  })
})
