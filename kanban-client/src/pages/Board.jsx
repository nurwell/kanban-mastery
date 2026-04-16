import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { DragDropContext } from '@hello-pangea/dnd';
import api from '../services/api';
import Column from '../components/Column';
import InviteModal from '../components/InviteModal';
import CardModal from '../components/CardModal';

export default function Board() {
  const { boardId } = useParams();
  const [board, setBoard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dragError, setDragError] = useState('');
  const [inviteOpen, setInviteOpen] = useState(false);
  const [newColTitle, setNewColTitle] = useState('');
  const [addingCol, setAddingCol] = useState(false);
  const [modalCard, setModalCard] = useState(null);
  const [members, setMembers] = useState([]);

  useEffect(() => {
    api.get(`/api/boards/${boardId}/members`)
      .then((res) => setMembers(res.data))
      .catch(() => {});
  }, [boardId]);

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
      const isCrossColumn = source.droppableId !== destColumnId;
      if (isCrossColumn) {
        const card = originalBoard.columns
          .find((c) => c.id.toString() === source.droppableId)?.cards[source.index];
        if (card) {
          await api.put(`/api/boards/${boardId}/cards/${cardId}`, {
            title: card.title,
            description: card.description ?? '',
            columnId: parseInt(destColumnId),
            position: destination.index,
          });
        }
      }
      // Always persist destination column order
      setBoard((current) => {
        const dstCol = current.columns.find((c) => c.id.toString() === destColumnId);
        if (dstCol) {
          api.put(`/api/boards/${boardId}/columns/${dstCol.id}/reorder`, {
            cardIds: dstCol.cards.map((c) => c.id),
          }).catch(() => {});
        }
        if (isCrossColumn) {
          const srcCol = current.columns.find((c) => c.id.toString() === source.droppableId);
          if (srcCol) {
            api.put(`/api/boards/${boardId}/columns/${srcCol.id}/reorder`, {
              cardIds: srcCol.cards.map((c) => c.id),
            }).catch(() => {});
          }
        }
        return current;
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
  const handleUpdateCard = async (cardId, title, description, assignedToUserId) => {
    const col = board.columns.find((c) => c.cards.some((card) => card.id === cardId));
    if (!col) return;
    const payload = { title, description, columnId: col.id };
    if (assignedToUserId !== undefined) payload.assignedToUserId = assignedToUserId;
    await api.put(`/api/boards/${boardId}/cards/${cardId}`, payload);
    setBoard((prev) => ({
      ...prev,
      columns: prev.columns.map((c) => ({
        ...c,
        cards: c.cards.map((card) =>
          card.id === cardId
            ? { ...card, title, description, ...(assignedToUserId !== undefined ? { assignedToUserId } : {}) }
            : card
        ),
      })),
    }));
    if (modalCard?.id === cardId) {
      setModalCard((prev) => prev ? { ...prev, title, description, ...(assignedToUserId !== undefined ? { assignedToUserId } : {}) } : null);
    }
  };

  const handleOpenModal = (card) => setModalCard(card);

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

      {loading && (
        <div className="columns-container">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="column-skeleton" style={{ animationDelay: `${i * 0.07}s` }}>
              <div className="skeleton skeleton-col-title" />
              <div className="skeleton skeleton-card" />
              <div className="skeleton skeleton-card skeleton-card--sm" />
              <div className="skeleton skeleton-card" />
            </div>
          ))}
        </div>
      )}
      {!loading && error && <p className="error" style={{ margin: '1.5rem' }}>{error}</p>}
      {dragError && <p className="error" style={{ margin: '0 1.5rem' }}>{dragError}</p>}

      {!loading && !error && board && (
        <DragDropContext onDragEnd={onDragEnd}>
          <div className="columns-container">
            {board.columns.length === 0 && (
              <div className="empty-state" style={{ flex: 1 }}>
                <div className="empty-icon">
                  <div className="empty-icon-col" />
                  <div className="empty-icon-col" />
                  <div className="empty-icon-col" />
                </div>
                <p className="empty-title">No columns yet</p>
                <p>Add your first column to start organizing work.</p>
              </div>
            )}
            {board.columns.map((col, index) => (
              <Column
                key={col.id}
                id={col.id}
                title={col.title}
                cards={col.cards}
                colIndex={index}
                onCreateCard={handleCreateCard}
                onDeleteCard={handleDeleteCard}
                onUpdateCard={handleUpdateCard}
                onDeleteColumn={handleDeleteColumn}
                onRenameColumn={handleRenameColumn}
                onOpenCard={handleOpenModal}
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

      {modalCard && (
        <CardModal
          card={modalCard}
          members={members}
          onUpdate={handleUpdateCard}
          onDelete={(id) => { handleDeleteCard(id); setModalCard(null); }}
          onClose={() => setModalCard(null)}
        />
      )}
    </div>
  );
}
