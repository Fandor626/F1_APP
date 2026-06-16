import { useRouteError } from 'react-router-dom'

export function ErrorBoundary() {
  useRouteError()

  return (
    <div role="alert" className="flex min-h-svh items-center justify-center p-8 text-center">
      <p className="text-text-secondary text-[13px]">Something went wrong — try refreshing.</p>
    </div>
  )
}
