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
    const controller = new AbortController();
    const { signal } = controller;

    Promise.all([
      api.get(`/api/boards/${boardId}`, { signal }),
      api.get(`/api/boards/${boardId}/members`, { signal }),
    ])
      .then(([boardRes, membersRes]) => {
        setBoard(boardRes.data);
        setMembers(membersRes.data);
      })
      .catch((err) => {
        if (err.name !== 'CanceledError') setError('Failed to load board');
      })
      .finally(() => setLoading(false));

    return () => controller.abort();
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

    // Compute optimistic next state up front
    const nextColumns = board.columns.map((col) => ({ ...col, cards: [...col.cards] }));
    const srcCol = nextColumns.find((c) => c.id.toString() === source.droppableId);
    const dstCol = nextColumns.find((c) => c.id.toString() === destColumnId);
    const [moved] = srcCol.cards.splice(source.index, 1);
    dstCol.cards.splice(destination.index, 0, moved);
    const nextBoard = { ...board, columns: nextColumns };

    setBoard(nextBoard);
    setDragError('');

    const isCrossColumn = source.droppableId !== destColumnId;
    try {
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
      // Persist destination column order
      await api.put(`/api/boards/${boardId}/columns/${dstCol.id}/reorder`, {
        cardIds: dstCol.cards.map((c) => c.id),
      });
      if (isCrossColumn) {
        await api.put(`/api/boards/${boardId}/columns/${srcCol.id}/reorder`, {
          cardIds: srcCol.cards.map((c) => c.id),
        });
      }
    } catch (_) {
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
    } catch (_) {
      setDragError('Failed to create column.');
    } finally {
      setAddingCol(false);
    }
  };

  // ── Create card ──────────────────────────────────────────────────
  const handleCreateCard = async (columnId, title) => {
    try {
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
    } catch (_) {
      setDragError('Failed to create card.');
      throw _;
    }
  };

  // ── Update card ───────────────────────────────────────────────────
  const handleUpdateCard = async (cardId, title, description, assignedToUserId) => {
    const col = board.columns.find((c) => c.cards.some((card) => card.id === cardId));
    if (!col) return;
    const payload = { title, description, columnId: col.id };
    if (assignedToUserId !== undefined) payload.assignedToUserId = assignedToUserId;
    const res = await api.put(`/api/boards/${boardId}/cards/${cardId}`, payload);
    const updated = res.data;
    setBoard((prev) => ({
      ...prev,
      columns: prev.columns.map((c) => ({
        ...c,
        cards: c.cards.map((card) => card.id === cardId ? { ...card, ...updated } : card),
      })),
    }));
    if (modalCard?.id === cardId) {
      setModalCard((prev) => prev ? { ...prev, ...updated } : null);
    }
  };

  const handleOpenModal = (card) => setModalCard(card);

  // ── Delete card ───────────────────────────────────────────────────
  const handleDeleteCard = async (cardId) => {
    try {
      await api.delete(`/api/boards/${boardId}/cards/${cardId}`);
      setBoard((prev) => ({
        ...prev,
        columns: prev.columns.map((col) => ({
          ...col,
          cards: col.cards.filter((card) => card.id !== cardId),
        })),
      }));
    } catch (_) {
      setDragError('Failed to delete card.');
    }
  };

  // ── Rename column ─────────────────────────────────────────────────
  const handleRenameColumn = async (columnId, title) => {
    try {
      await api.put(`/api/boards/${boardId}/columns/${columnId}`, { title });
      setBoard((prev) => ({
        ...prev,
        columns: prev.columns.map((col) => col.id === columnId ? { ...col, title } : col),
      }));
    } catch (_) {
      setDragError('Failed to rename column.');
      throw _;
    }
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
