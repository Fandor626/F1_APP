import { render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TrackOutline } from './TrackOutline'

const mockConfig = {
  circuitId: 'monza',
  viewBox: '0 0 500 500',
  trackPath: 'M10,10 L90,90 Z',
}

beforeEach(() => {
  vi.resetAllMocks()
  globalThis.fetch = vi.fn()
})

describe('TrackOutline', () => {
  it('fetches the circuit config from a relative same-origin path and renders the track shape', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockConfig),
    } as Response)

    render(<TrackOutline circuitId="monza" circuitName="Autodromo Nazionale Monza" />)

    await waitFor(() => expect(screen.getByRole('img', { name: 'Track layout: Autodromo Nazionale Monza' })).toBeInTheDocument())

    const requestedUrl = vi.mocked(globalThis.fetch).mock.calls[0][0]
    expect(requestedUrl).toBe('/circuit-configs/monza.json')
  })

  it('renders nothing when the config fetch fails', async () => {
    vi.mocked(globalThis.fetch).mockResolvedValue({ ok: false } as Response)

    const { container } = render(<TrackOutline circuitId="unknown" circuitName="Unknown Circuit" />)

    await waitFor(() => expect(vi.mocked(globalThis.fetch)).toHaveBeenCalled())
    expect(container.firstChild).toBeNull()
  })

  it('renders nothing while the config is still loading', () => {
    vi.mocked(globalThis.fetch).mockReturnValue(new Promise(() => {}))

    const { container } = render(<TrackOutline circuitId="monza" circuitName="Monza" />)

    expect(container.firstChild).toBeNull()
  })
})
