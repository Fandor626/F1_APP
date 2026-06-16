import { http, HttpResponse } from 'msw'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export const healthHandlers = [
  http.get(`${API_BASE_URL}/api/health`, () => HttpResponse.json({ status: 'ok' })),
]
