import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { queryKeys } from './queryKeys'

const NewsItemSchema = z.object({
  title: z.string(),
  link: z.string(),
  source: z.string(),
  publishedAt: z.string(),
})

const NewsFeedSchema = z.array(NewsItemSchema)

export type NewsItem = z.infer<typeof NewsItemSchema>

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined
const REQUEST_TIMEOUT_MS = 10_000
// Mirrors backend's default 15-minute refresh interval.
const NEWS_STALE_TIME_MS = 1000 * 60 * 15

export function useNewsFeed() {
  return useQuery({
    queryKey: queryKeys.news,
    queryFn: async ({ signal }) => {
      if (!API_BASE_URL) {
        throw new Error('VITE_API_BASE_URL is not set — copy .env.example to .env.local')
      }
      const response = await fetch(`${API_BASE_URL}/api/news`, {
        signal: AbortSignal.any([signal, AbortSignal.timeout(REQUEST_TIMEOUT_MS)]),
      })
      if (!response.ok) {
        throw new Error(`Request to /api/news failed: ${response.status}`)
      }
      return NewsFeedSchema.parse(await response.json())
    },
    staleTime: NEWS_STALE_TIME_MS,
    retry: false,
  })
}
