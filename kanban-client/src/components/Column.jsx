import { useState } from 'react';
import { Droppable } from '@hello-pangea/dnd';
import Card from './Card';

export default function Column({ id, title, cards, onCreateCard, onDeleteCard, onUpdateCard, onDeleteColumn, onRenameColumn }) {
  const [adding, setAdding] = useState(false);
  const [title_, setTitle_] = useState('');
  const [loading, setLoading] = useState(false);
  const [renamingCol, setRenamingCol] = useState(false);
  const [colTitle, setColTitle] = useState(title);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!title_.trim()) return;
    setLoading(true);
    await onCreateCard(id, title_.trim());
    setTitle_('');
    setAdding(false);
    setLoading(false);
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Escape') {
      setAdding(false);
      setTitle_('');
    }
  };

  const handleRenameBlur = async () => {
    const trimmed = colTitle.trim();
    if (!trimmed || trimmed === title) { setRenamingCol(false); setColTitle(title); return; }
    await onRenameColumn(id, trimmed);
    setRenamingCol(false);
  };

  return (
    <div className="column">
      <div className="column-header">
        {renamingCol ? (
          <input
            className="column-rename-input"
            value={colTitle}
            onChange={(e) => setColTitle(e.target.value)}
            onBlur={handleRenameBlur}
            onKeyDown={(e) => {
              if (e.key === 'Enter') handleRenameBlur();
              if (e.key === 'Escape') { setRenamingCol(false); setColTitle(title); }
            }}
            autoFocus
          />
        ) : (
          <h2 className="column-title" onClick={() => setRenamingCol(true)} title="Click to rename">{title}</h2>
        )}
        <button className="column-delete-btn" onClick={() => onDeleteColumn(id)} title="Delete column">×</button>
      </div>

      <Droppable droppableId={id.toString()}>
        {(provided) => (
          <div
            className="card-stack"
            ref={provided.innerRef}
            {...provided.droppableProps}
          >
            {cards.length === 0 && !adding && (
              <p className="column-empty">No cards</p>
            )}
            {cards.map((card, index) => (
              <Card
                key={card.id}
                id={card.id}
                index={index}
                title={card.title}
                description={card.description}
                assignedToUserId={card.assignedToUserId}
                onDelete={onDeleteCard}
                onUpdate={onUpdateCard}
              />
            ))}
            {provided.placeholder}
          </div>
        )}
      </Droppable>

      {adding ? (
        <form className="add-card-form" onSubmit={handleSubmit}>
          <input
            type="text"
            className="add-card-input"
            placeholder="Card title…"
            value={title_}
            onChange={(e) => setTitle_(e.target.value)}
            onKeyDown={handleKeyDown}
            autoFocus
          />
          <div className="add-card-actions">
            <button type="submit" disabled={loading || !title_.trim()}>
              {loading ? 'Adding…' : 'Add card'}
            </button>
            <button
              type="button"
              className="btn-secondary"
              onClick={() => { setAdding(false); setTitle_(''); }}
            >
              Cancel
            </button>
          </div>
        </form>
      ) : (
        <button className="add-card-btn" onClick={() => setAdding(true)}>
          + Add card
        </button>
      )}
    </div>
  );
}
