import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../services/api';
import Column from '../components/Column';

export default function Board() {
  const { boardId } = useParams();
  const [board, setBoard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get(`/api/boards/${boardId}`)
      .then((res) => setBoard(res.data))
      .catch(() => setError('Failed to load board'))
      .finally(() => setLoading(false));
  }, [boardId]);

  return (
    <div className="board-page">
      <header className="board-header">
        <Link to="/dashboard" className="back-link">← Boards</Link>
        {board && <h1 className="board-page-title">{board.name}</h1>}
      </header>

      {loading && <p className="status-message">Loading board…</p>}
      {!loading && error && <p className="error">{error}</p>}

      {!loading && !error && board && (
        <div className="columns-container">
          {board.columns.length === 0 && (
            <p className="status-message">No columns yet.</p>
          )}
          {board.columns.map((col) => (
            <Column key={col.id} title={col.title} cards={col.cards} />
          ))}
        </div>
      )}
    </div>
  );
}
