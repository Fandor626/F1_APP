import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { describe, expect, it } from 'vitest'
import App from './App'
import { server } from './shared/test/server'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

function renderApp() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>,
  )
}

describe('App health check round trip', () => {
  it('renders the backend health status once the request resolves', async () => {
    renderApp()

    expect(screen.getByTestId('health-status')).toHaveTextContent('Checking backend')

    await waitFor(() =>
      expect(screen.getByTestId('health-status')).toHaveTextContent('Backend status: ok'),
    )
  })

  it('shows an unreachable message when the network request fails', async () => {
    server.use(
      http.get(`${API_BASE_URL}/api/health`, () => HttpResponse.error()),
    )

    renderApp()

    await waitFor(() =>
      expect(screen.getByTestId('health-status')).toHaveTextContent('Backend unreachable'),
    )
  })

  it('shows an unreachable message when the backend responds with a non-2xx status', async () => {
    server.use(
      http.get(`${API_BASE_URL}/api/health`, () => new HttpResponse(null, { status: 500 })),
    )

    renderApp()

    await waitFor(() =>
      expect(screen.getByTestId('health-status')).toHaveTextContent('Backend unreachable'),
    )
  })
})
