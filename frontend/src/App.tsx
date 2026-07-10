import { Link, Outlet } from 'react-router-dom'

function App() {
  return (
    <>
      <nav className="h-14 bg-[#1b1f26] border-b border-[#2a2f38] flex items-center gap-6 px-6">
        <Link
          to="/"
          className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]"
        >
          Calendar
        </Link>
        <Link
          to="/live"
          className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]"
        >
          Live Race
        </Link>
        <Link
          to="/standings"
          className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]"
        >
          Standings
        </Link>
      </nav>
      <Outlet />
    </>
  )
}

export default App
