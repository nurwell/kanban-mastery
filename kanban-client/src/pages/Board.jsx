import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { DragDropContext } from '@hello-pangea/dnd';
import api from '../services/api';
import Column from '../components/Column';

export default function Board() {
  const { boardId } = useParams();
  const [board, setBoard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dragError, setDragError] = useState('');

  useEffect(() => {
    api.get(`/api/boards/${boardId}`)
      .then((res) => setBoard(res.data))
      .catch(() => setError('Failed to load board'))
      .finally(() => setLoading(false));
  }, [boardId]);

  const onDragEnd = async (result) => {
    const { draggableId, source, destination } = result;

    if (!destination) return;
    if (
      source.droppableId === destination.droppableId &&
      source.index === destination.index
    ) return;

    const cardId = draggableId;
    const destColumnId = destination.droppableId;

    // Snapshot original state for rollback
    const originalBoard = board;

    // Optimistic update
    setBoard((prev) => {
      const columns = prev.columns.map((col) => ({ ...col, cards: [...col.cards] }));
      const srcCol = columns.find((c) => c.id.toString() === source.droppableId);
      const dstCol = columns.find((c) => c.id.toString() === destColumnId);
      const [moved] = srcCol.cards.splice(source.index, 1);
      dstCol.cards.splice(destination.index, 0, moved);
      return { ...prev, columns };
    });

    setDragError('');

    try {
      await api.put(`/api/boards/${boardId}/cards/${cardId}`, {
        columnId: parseInt(destColumnId),
      });
    } catch {
      setBoard(originalBoard);
      setDragError('Failed to move card. Change reverted.');
    }
  };

  return (
    <div className="board-page">
      <header className="board-header">
        <Link to="/dashboard" className="back-link">← Boards</Link>
        {board && <h1 className="board-page-title">{board.name}</h1>}
      </header>

      {loading && <p className="status-message">Loading board…</p>}
      {!loading && error && <p className="error">{error}</p>}
      {dragError && <p className="error">{dragError}</p>}

      {!loading && !error && board && (
        <DragDropContext onDragEnd={onDragEnd}>
          <div className="columns-container">
            {board.columns.length === 0 && (
              <p className="status-message">No columns yet.</p>
            )}
            {board.columns.map((col) => (
              <Column key={col.id} id={col.id} title={col.title} cards={col.cards} />
            ))}
          </div>
        </DragDropContext>
      )}
    </div>
  );
}
