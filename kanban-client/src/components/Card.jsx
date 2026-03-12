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

export default function Card({ id, index, title, description, assignedToUserId }) {
  return (
    <Draggable draggableId={id.toString()} index={index}>
      {(provided, snapshot) => (
        <div
          className={`card${snapshot.isDragging ? ' card--dragging' : ''}`}
          ref={provided.innerRef}
          {...provided.draggableProps}
          {...provided.dragHandleProps}
        >
          <p className="card-title">{title}</p>
          {description && <p className="card-description">{description}</p>}
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
      )}
    </Draggable>
  );
}
