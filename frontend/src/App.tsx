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
        <Link
          to="/news"
          className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]"
        >
          News Feed
        </Link>
        <Link
          to="/fan-card"
          className="text-[#eef0f3] text-sm font-semibold hover:text-[#d8b65c]"
        >
          Fan Card
        </Link>
      </nav>
      <Outlet />
      <footer className="border-t border-[#2a2f38] px-6 py-4 text-[11px] text-[#9aa1ad]">
        Track outlines:{' '}
        <a
          href="https://github.com/f1db/f1db"
          target="_blank"
          rel="noreferrer"
          className="hover:text-[#d8b65c]"
        >
          f1db/f1db
        </a>
        , CC-BY-4.0
      </footer>
    </>
  )
}

export default App
