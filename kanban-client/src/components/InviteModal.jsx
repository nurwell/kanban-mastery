import { useEffect, useRef, useState } from 'react';
import Modal from 'react-modal';
import api from '../services/api';

Modal.setAppElement('#root');

export default function InviteModal({ boardId, isOpen, onClose }) {
  const [email, setEmail] = useState('');
  const [status, setStatus] = useState(null); // { type: 'success'|'error', message }
  const [loading, setLoading] = useState(false);
  const inputRef = useRef(null);

  // Reset form when modal opens
  useEffect(() => {
    if (isOpen) {
      setEmail('');
      setStatus(null);
      setTimeout(() => inputRef.current?.focus(), 50);
    }
  }, [isOpen]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!email.trim()) return;
    setLoading(true);
    setStatus(null);

    try {
      // Resolve email → userId
      const lookupRes = await api.get('/api/users/lookup', { params: { email: email.trim() } });
      const userId = lookupRes.data.id;

      // Invite the member
      await api.post(`/api/boards/${boardId}/members`, { userId });

      setStatus({ type: 'success', message: `${email.trim()} has been added to the board.` });
      setEmail('');
    } catch (err) {
      const message =
        err.response?.status === 404
          ? 'No account found with that email.'
          : err.response?.status === 403
          ? 'Only the board owner can invite members.'
          : 'Failed to invite user.';
      setStatus({ type: 'error', message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onRequestClose={onClose}
      className="modal"
      overlayClassName="modal-overlay"
      contentLabel="Invite a teammate"
    >
      <div className="modal-header">
        <h2 className="modal-title">Invite a teammate</h2>
        <button className="modal-close" onClick={onClose} aria-label="Close">×</button>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="invite-email">Email address</label>
          <input
            id="invite-email"
            ref={inputRef}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="teammate@example.com"
            required
          />
        </div>

        {status && (
          <p className={status.type === 'success' ? 'invite-success' : 'error'}>
            {status.message}
          </p>
        )}

        <div className="modal-actions">
          <button type="button" className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" disabled={loading || !email.trim()}>
            {loading ? 'Inviting…' : 'Send invite'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
