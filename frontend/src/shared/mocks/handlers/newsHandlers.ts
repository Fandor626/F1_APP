import { http, HttpResponse } from 'msw'
import type { NewsItem } from '../../api/news'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export const sampleNewsFeed: NewsItem[] = [
  {
    title: 'How Hadjar is taking the fight to untouchable Verstappen',
    link: 'https://www.formula1.com/en/latest/example-1',
    source: 'Formula1.com',
    publishedAt: '2026-07-10T10:00:00Z',
    imageUrl: 'https://www.formula1.com/en/latest/example-1/thumbnail.jpg',
    snippet: 'A closer look at how the rookie has closed the gap in recent races.',
  },
  {
    title: 'Alonso has "no idea" what he\'ll do when he eventually retires from F1',
    link: 'https://www.autosport.com/f1/news/example-2',
    source: 'Autosport',
    publishedAt: '2026-07-10T08:30:00Z',
  },
  {
    title: 'RaceFans roundup: what we learned this weekend',
    link: 'https://www.racefans.net/example-3',
    source: 'RaceFans',
    publishedAt: '2026-07-09T18:15:00Z',
  },
]

export const newsHandlers = [
  http.get(`${API_BASE_URL}/api/news`, () => HttpResponse.json(sampleNewsFeed)),
]
