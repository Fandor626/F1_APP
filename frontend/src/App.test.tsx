import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import App from './App'

function renderApp(initialPath = '/') {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <App />,
        children: [
          { index: true, element: <div>Home</div> },
          { path: 'standings', element: <div>Standings content</div> },
        ],
      },
    ],
    { initialEntries: [initialPath] },
  )
  return render(<RouterProvider router={router} />)
}

describe('App', () => {
  it('shows the f1db attribution footer exactly once', () => {
    renderApp()

    expect(screen.getAllByText(/f1db\/f1db/)).toHaveLength(1)
    expect(screen.getByText(/CC-BY-4.0/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'f1db/f1db' })).toHaveAttribute(
      'href',
      'https://github.com/f1db/f1db',
    )
  })

  it('keeps the attribution footer present on every route', () => {
    renderApp('/standings')

    expect(screen.getByText('Standings content')).toBeInTheDocument()
    expect(screen.getByText(/f1db\/f1db/)).toBeInTheDocument()
  })
})
