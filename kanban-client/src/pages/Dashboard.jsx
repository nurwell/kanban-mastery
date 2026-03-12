import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function Dashboard() {
  const navigate = useNavigate();
  const [boards, setBoards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);

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
    navigate('/login');
  };

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h1>My Boards</h1>
        <button className="logout-btn" onClick={handleLogout}>Log out</button>
      </div>

      <form className="create-board-form" onSubmit={handleCreate}>
        <input
          type="text"
          placeholder="New board name…"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
        />
        <button type="submit" disabled={creating || !newName.trim()}>
          {creating ? 'Creating…' : 'Create'}
        </button>
      </form>

      {error && <p className="error">{error}</p>}
      {loading && <p className="status-message">Loading…</p>}

      {!loading && !error && boards.length === 0 && (
        <div className="empty-state">
          <p>No boards yet.</p>
          <p>Create one above to get started.</p>
        </div>
      )}

      {!loading && boards.length > 0 && (
        <ul className="board-list">
          {boards.map((board) => (
            <li key={board.id}>
              <Link to={`/boards/${board.id}`} className="board-card">
                <span className="board-name">{board.name}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
