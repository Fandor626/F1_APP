import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { queryKeys } from './queryKeys'

const HealthResponseSchema = z.object({
  status: z.string(),
})

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

async function fetchHealth() {
  const response = await fetch(`${API_BASE_URL}/api/health`)
  if (!response.ok) {
    throw new Error(`Health check failed: ${response.status}`)
  }
  return HealthResponseSchema.parse(await response.json())
}

export function useHealthCheck() {
  return useQuery({
    queryKey: queryKeys.health,
    queryFn: fetchHealth,
    retry: false,
  })
}
