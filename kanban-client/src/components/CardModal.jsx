import { useState, useEffect } from 'react';

export default function CardModal({ card, members, onUpdate, onDelete, onClose }) {
  const [title, setTitle] = useState(card.title);
  const [description, setDescription] = useState(card.description ?? '');
  const [assignedTo, setAssignedTo] = useState(card.assignedToUserId ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setTitle(card.title);
    setDescription(card.description ?? '');
    setAssignedTo(card.assignedToUserId ?? '');
  }, [card.id]);

  const handleSave = async () => {
    const trimmed = title.trim();
    if (!trimmed) return;
    setSaving(true);
    setError('');
    try {
      await onUpdate(card.id, trimmed, description, assignedTo || null);
    } catch {
      setError('Failed to save changes.');
    } finally {
      setSaving(false);
    }
  };

  const handleOverlayKey = (e) => {
    if (e.key === 'Escape') onClose();
  };

  return (
    <div
      className="modal-overlay"
      onClick={onClose}
      onKeyDown={handleOverlayKey}
      role="dialog"
      aria-modal="true"
    >
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="mc-header">
          <input
            className="modal-title-input"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Card title"
            autoFocus
          />
          <button className="modal-close-btn" onClick={onClose} title="Close">×</button>
        </div>

        <div className="mc-body">
          <div>
            <label className="modal-label">Description</label>
            <textarea
              className="modal-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Add a description…"
              rows={4}
            />
          </div>

          {members.length > 0 && (
            <div>
              <label className="modal-label">Assignee</label>
              <select
                className="modal-select"
                value={assignedTo}
                onChange={(e) => setAssignedTo(e.target.value)}
              >
                <option value="">Unassigned</option>
                {members.map((m) => (
                  <option key={m.userId} value={m.userId}>
                    {m.email ?? m.userName ?? m.userId}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>

        {error && <p className="modal-error">{error}</p>}

        <div className="mc-footer">
          <button className="btn-danger" onClick={() => onDelete(card.id)} disabled={saving}>
            Delete card
          </button>
          <div className="mc-footer-right">
            <button className="btn-secondary" onClick={onClose} disabled={saving}>
              Cancel
            </button>
            <button className="btn-primary" onClick={handleSave} disabled={saving || !title.trim()}>
              {saving ? 'Saving…' : 'Save changes'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
