import { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import api from '../services/api';

export default function Dashboard() {
  const navigate = useNavigate();
  const [boards, setBoards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function fetchBoards() {
      try {
        const meResponse = await api.get('/api/users/me');
        const userId = meResponse.data.id;
        const boardsResponse = await api.get('/api/boards', { params: { userId } });
        setBoards(boardsResponse.data);
      } catch (err) {
        setError('Failed to load boards');
      } finally {
        setLoading(false);
      }
    }
    fetchBoards();
  }, []);

  function handleLogout() {
    localStorage.removeItem('token');
    navigate('/login');
  }

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>My Boards</h1>
        <button className="logout-btn" onClick={handleLogout}>Sign out</button>
      </header>

      {loading && <p className="status-message">Loading boards…</p>}

      {!loading && error && <p className="error">{error}</p>}

      {!loading && !error && boards.length === 0 && (
        <div className="empty-state">
          <p>No boards yet.</p>
          <p>Create your first board to get started.</p>
        </div>
      )}

      {!loading && !error && boards.length > 0 && (
        <ul className="board-list">
          {boards.map((board) => (
            <li key={board.id}>
              <Link to={`/board/${board.id}`} className="board-card">
                <span className="board-name">{board.name}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
