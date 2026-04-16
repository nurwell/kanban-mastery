import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function Register() {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await api.post('/register', { email, password });
      navigate('/login');
    } catch (err) {
      const detail = err.response?.data?.errors
        ? Object.values(err.response.data.errors).flat().join(' ')
        : 'Registration failed';
      setError(detail);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-left">
        <div className="auth-brand">
          <div className="auth-brand-name">
            <div className="auth-logo-mark">
              <div className="auth-logo-bar" />
              <div className="auth-logo-bar" />
              <div className="auth-logo-bar" />
            </div>
            Flowboard
          </div>
          <p className="auth-tagline">Organize · Prioritize · Deliver</p>
        </div>

        <div className="kanban-preview">
          <div className="kp-col">
            <div className="kp-col-title" />
            <div className="kp-card" />
            <div className="kp-card kp-card--sm" />
            <div className="kp-card" />
          </div>
          <div className="kp-col">
            <div className="kp-col-title" />
            <div className="kp-card kp-card--active" />
            <div className="kp-card kp-card--active" />
            <div className="kp-card kp-card--sm" />
          </div>
          <div className="kp-col">
            <div className="kp-col-title" />
            <div className="kp-card kp-card--sm" />
            <div className="kp-card" />
          </div>
        </div>
      </div>

      <div className="auth-right">
        <div className="auth-card">
          <div className="auth-logo">
            <div className="auth-logo-icon">
              <div className="auth-logo-bar" />
              <div className="auth-logo-bar" />
              <div className="auth-logo-bar" />
            </div>
            <span>Flowboard</span>
          </div>
          <p className="auth-subtitle">Create your free account.</p>

          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="reg-email">Email</label>
              <input
                id="reg-email"
                type="email"
                value={email}
                placeholder="you@example.com"
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>
            <div className="form-group">
              <label htmlFor="reg-password">Password</label>
              <input
                id="reg-password"
                type="password"
                value={password}
                placeholder="••••••••"
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="new-password"
              />
            </div>
            {error && <p className="error">{error}</p>}
            <button type="submit" disabled={loading}>
              {loading ? 'Creating account…' : 'Create account'}
            </button>
          </form>
          <p>Already have an account? <Link to="/login">Sign in</Link></p>
        </div>
      </div>
    </div>
  );
}
