import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';
import useTheme from '../hooks/useTheme';

const ACCENT_COLORS = ['#3b82f6', '#8b5cf6', '#10b981', '#f59e0b', '#ec4899', '#14b8a6'];

export default function Dashboard() {
  const navigate = useNavigate();
  const [isDark, toggleTheme] = useTheme();
  const [boards, setBoards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);
  const [search, setSearch] = useState('');
  const userEmail = localStorage.getItem('userEmail') ?? '';

  const filteredBoards = search.trim()
    ? boards.filter((b) => b.name.toLowerCase().includes(search.trim().toLowerCase()))
    : boards;

  useEffect(() => {
    api.get('/api/boards')
      .then((res) => setBoards(res.data))
      .catch(() => setError('Failed to load boards'))
      .finally(() => setLoading(false));
  }, []);

  const handleCreate = async (e) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setCreating(true);
    setError('');
    try {
      const res = await api.post('/api/boards', { name: newName.trim() });
      setBoards((prev) => [...prev, res.data]);
      setNewName('');
    } catch {
      setError('Failed to create board');
    } finally {
      setCreating(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userEmail');
    navigate('/login');
  };

  return (
    <div className="page-shell">
      <nav className="navbar">
        <div className="navbar-logo">
          <div className="navbar-logo-icon">
            <div className="navbar-logo-bar" />
            <div className="navbar-logo-bar" />
            <div className="navbar-logo-bar" />
          </div>
          Flowboard
        </div>

        <div className="navbar-search">
          <span className="navbar-search-icon">⌕</span>
          <input
            type="text"
            placeholder="Search boards…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="navbar-user">
          <button
            className="theme-toggle"
            onClick={toggleTheme}
            title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {isDark ? '☀' : '☽'}
          </button>
          {userEmail && (
            <>
              <div className="user-avatar">{userEmail[0].toUpperCase()}</div>
              <span className="user-email">{userEmail}</span>
            </>
          )}
          <button className="logout-btn" onClick={handleLogout}>Log out</button>
        </div>
      </nav>

      <main className="dashboard">
        <h1>My Boards</h1>

        <form className="create-board-form" onSubmit={handleCreate}>
          <input
            type="text"
            placeholder="Name your new board…"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
          />
          <button type="submit" disabled={creating || !newName.trim()}>
            {creating ? 'Creating…' : 'Create board'}
          </button>
        </form>

        {error && <p className="error">{error}</p>}

        {loading && (
          <ul className="board-list">
            {[...Array(6)].map((_, i) => (
              <li key={i}>
                <div className="board-card-skeleton" style={{ animationDelay: `${i * 0.05}s` }}>
                  <div className="skeleton skeleton-band" />
                  <div className="skeleton skeleton-title" />
                  <div className="skeleton skeleton-meta" />
                </div>
              </li>
            ))}
          </ul>
        )}

        {!loading && !error && boards.length === 0 && (
          <div className="empty-state">
            <div className="empty-icon">
              <div className="empty-icon-col" />
              <div className="empty-icon-col" />
              <div className="empty-icon-col" />
            </div>
            <p className="empty-title">No boards yet</p>
            <p>Create your first board above to get started.</p>
          </div>
        )}

        {!loading && boards.length > 0 && filteredBoards.length === 0 && (
          <div className="empty-state">
            <p className="empty-title">No matches</p>
            <p>No boards found for &ldquo;{search}&rdquo;.</p>
          </div>
        )}

        {!loading && filteredBoards.length > 0 && (
          <ul className="board-list">
            {filteredBoards.map((board, i) => (
              <li key={board.id} style={{ animationDelay: `${i * 0.04}s` }}>
                <Link
                  to={`/boards/${board.id}`}
                  className="board-card"
                  data-accent={board.id % ACCENT_COLORS.length}
                >
                  <span className="board-name">{board.name}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
