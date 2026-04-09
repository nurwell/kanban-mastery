import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { DragDropContext } from '@hello-pangea/dnd';
import api from '../services/api';
import Column from '../components/Column';
import InviteModal from '../components/InviteModal';

export default function Board() {
  const { boardId } = useParams();
  const [board, setBoard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dragError, setDragError] = useState('');
  const [inviteOpen, setInviteOpen] = useState(false);
  const [newColTitle, setNewColTitle] = useState('');
  const [addingCol, setAddingCol] = useState(false);

  useEffect(() => {
    api.get(`/api/boards/${boardId}`)
      .then((res) => setBoard(res.data))
      .catch(() => setError('Failed to load board'))
      .finally(() => setLoading(false));
  }, [boardId]);

  // ── Drag-and-drop ────────────────────────────────────────────────
  const onDragEnd = async (result) => {
    const { draggableId, source, destination } = result;

    if (!destination) return;
    if (
      source.droppableId === destination.droppableId &&
      source.index === destination.index
    ) return;

    const cardId = draggableId;
    const destColumnId = destination.droppableId;
    const originalBoard = board;

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

  // ── Create column ────────────────────────────────────────────────
  const handleCreateColumn = async (e) => {
    e.preventDefault();
    if (!newColTitle.trim()) return;
    setAddingCol(true);
    try {
      const res = await api.post(`/api/boards/${boardId}/columns`, { title: newColTitle.trim() });
      setBoard((prev) => ({ ...prev, columns: [...prev.columns, { ...res.data, cards: [] }] }));
      setNewColTitle('');
    } finally {
      setAddingCol(false);
    }
  };

  // ── Create card ──────────────────────────────────────────────────
  const handleCreateCard = async (columnId, title) => {
    const res = await api.post(`/api/boards/${boardId}/cards`, { title, columnId });
    const newCard = res.data;
    setBoard((prev) => ({
      ...prev,
      columns: prev.columns.map((col) =>
        col.id === columnId
          ? { ...col, cards: [...col.cards, newCard] }
          : col
      ),
    }));
  };

  // ── Update card ───────────────────────────────────────────────────
  const handleUpdateCard = async (cardId, title, description) => {
    const col = board.columns.find((c) => c.cards.some((card) => card.id === cardId));
    if (!col) return;
    await api.put(`/api/boards/${boardId}/cards/${cardId}`, { title, description, columnId: col.id });
    setBoard((prev) => ({
      ...prev,
      columns: prev.columns.map((c) => ({
        ...c,
        cards: c.cards.map((card) => card.id === cardId ? { ...card, title, description } : card),
      })),
    }));
  };

  // ── Delete card ───────────────────────────────────────────────────
  const handleDeleteCard = async (cardId) => {
    await api.delete(`/api/boards/${boardId}/cards/${cardId}`);
    setBoard((prev) => ({
      ...prev,
      columns: prev.columns.map((col) => ({
        ...col,
        cards: col.cards.filter((card) => card.id !== cardId),
      })),
    }));
  };

  // ── Rename column ─────────────────────────────────────────────────
  const handleRenameColumn = async (columnId, title) => {
    await api.put(`/api/boards/${boardId}/columns/${columnId}`, { title });
    setBoard((prev) => ({
      ...prev,
      columns: prev.columns.map((col) => col.id === columnId ? { ...col, title } : col),
    }));
  };

  // ── Delete column ─────────────────────────────────────────────────
  const handleDeleteColumn = async (columnId) => {
    try {
      await api.delete(`/api/boards/${boardId}/columns/${columnId}`);
      setBoard((prev) => ({
        ...prev,
        columns: prev.columns.filter((col) => col.id !== columnId),
      }));
    } catch (err) {
      const msg = err.response?.data ?? 'Cannot delete column';
      setDragError(msg);
    }
  };

  return (
    <div className="board-page">
      <header className="board-header">
        <Link to="/dashboard" className="back-link">← Boards</Link>
        {board && <h1 className="board-page-title">{board.name}</h1>}
        {board && (
          <button className="invite-btn" onClick={() => setInviteOpen(true)}>
            Invite
          </button>
        )}
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
              <Column
                key={col.id}
                id={col.id}
                title={col.title}
                cards={col.cards}
                onCreateCard={handleCreateCard}
                onDeleteCard={handleDeleteCard}
                onUpdateCard={handleUpdateCard}
                onDeleteColumn={handleDeleteColumn}
                onRenameColumn={handleRenameColumn}
              />
            ))}
            <div className="add-column-form">
              <form onSubmit={handleCreateColumn}>
                <input
                  type="text"
                  placeholder="New column title…"
                  value={newColTitle}
                  onChange={(e) => setNewColTitle(e.target.value)}
                />
                <button type="submit" disabled={addingCol || !newColTitle.trim()}>
                  {addingCol ? 'Adding…' : '+ Add Column'}
                </button>
              </form>
            </div>
          </div>
        </DragDropContext>
      )}

      <InviteModal
        boardId={boardId}
        isOpen={inviteOpen}
        onClose={() => setInviteOpen(false)}
      />
    </div>
  );
}
