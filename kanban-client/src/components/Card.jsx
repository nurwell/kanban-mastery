import { useState } from 'react';
import { Draggable } from '@hello-pangea/dnd';

function avatarColor(userId) {
  const palette = [
    '#6366f1', '#8b5cf6', '#ec4899', '#f59e0b',
    '#10b981', '#3b82f6', '#ef4444', '#14b8a6',
  ];
  let hash = 0;
  for (let i = 0; i < userId.length; i++) {
    hash = userId.charCodeAt(i) + ((hash << 5) - hash);
  }
  return palette[Math.abs(hash) % palette.length];
}

function avatarLabel(userId) {
  return userId.slice(0, 2).toUpperCase();
}

export default function Card({ id, index, title, description, assignedToUserId, onDelete, onUpdate, onOpen, colIndex }) {
  const [editing, setEditing] = useState(false);
  const [editTitle, setEditTitle] = useState(title);

  const handleSave = async () => {
    const trimmed = editTitle.trim();
    if (!trimmed || trimmed === title) { setEditing(false); setEditTitle(title); return; }
    await onUpdate(id, trimmed, description);
    setEditing(false);
  };

  const handleTitleClick = () => {
    if (onOpen) { onOpen(); return; }
    setEditing(true);
  };

  return (
    <Draggable draggableId={id.toString()} index={index}>
      {(provided, snapshot) => (
        <div
          className={`card card--col-${(colIndex ?? 0) % 6}${snapshot.isDragging ? ' card--dragging' : ''}`}
          ref={provided.innerRef}
          {...provided.draggableProps}
          {...provided.dragHandleProps}
        >
          {editing ? (
            <input
              className="card-edit-input"
              value={editTitle}
              onChange={(e) => setEditTitle(e.target.value)}
              onBlur={handleSave}
              onKeyDown={(e) => {
                if (e.key === 'Enter') handleSave();
                if (e.key === 'Escape') { setEditing(false); setEditTitle(title); }
              }}
              autoFocus
            />
          ) : (
            <p className="card-title" onClick={handleTitleClick} title="Click to open">{title}</p>
          )}
          {description && <p className="card-description">{description}</p>}
          <div className="card-footer">
            <button
              className="card-delete-btn"
              onClick={() => onDelete(id)}
              title="Delete card"
            >
              ×
            </button>
            {assignedToUserId && (
              <div
                className="card-avatar"
                style={{ backgroundColor: avatarColor(assignedToUserId) }}
                title={assignedToUserId}
              >
                {avatarLabel(assignedToUserId)}
              </div>
            )}
          </div>
        </div>
      )}
    </Draggable>
  );
}
