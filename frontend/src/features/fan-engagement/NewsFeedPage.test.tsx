import { render, screen, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HttpResponse, http } from 'msw'
import { describe, expect, it } from 'vitest'
import { NewsFeedPage } from './NewsFeedPage'
import { server } from '../../shared/test/server'
import { sampleNewsFeed } from '../../shared/mocks/handlers/newsHandlers'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

function renderPage() {
  const queryClient = new QueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <NewsFeedPage />
    </QueryClientProvider>,
  )
}

describe('NewsFeedPage', () => {
  it('renders a card per headline with title and source', async () => {
    renderPage()

    await waitFor(() => expect(screen.getAllByTestId('news-card')).toHaveLength(sampleNewsFeed.length))

    for (const item of sampleNewsFeed) {
      expect(screen.getByText(item.title)).toBeInTheDocument()
    }
    expect(within(screen.getAllByTestId('news-card')[0]).getByText(/Formula1\.com/)).toBeInTheDocument()
  })

  it('opens each card in a new tab', async () => {
    renderPage()

    await waitFor(() => expect(screen.getAllByTestId('news-card')).toHaveLength(sampleNewsFeed.length))

    for (const card of screen.getAllByTestId('news-card')) {
      expect(card).toHaveAttribute('target', '_blank')
      expect(card).toHaveAttribute('rel', 'noopener noreferrer')
    }
  })

  it('shows a clear "no news available" state when all feeds are empty', async () => {
    server.use(http.get(`${API_BASE_URL}/api/news`, () => HttpResponse.json([])))

    renderPage()

    await waitFor(() => expect(screen.getByText('No news available right now.')).toBeInTheDocument())
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('shows the error state when the request fails outright', async () => {
    server.use(http.get(`${API_BASE_URL}/api/news`, () => HttpResponse.error()))

    renderPage()

    await waitFor(() =>
      expect(screen.getByText("Couldn't reach the server — try refreshing.")).toBeInTheDocument(),
    )
  })
})
