import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function Login() {
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
      const res = await api.post('/login', { email, password });
      localStorage.setItem('token', res.data.accessToken);
      localStorage.setItem('userEmail', email);
      navigate('/dashboard');
    } catch {
      setError('Invalid email or password');
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
          <p className="auth-subtitle">Welcome back — sign in to continue.</p>

          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="login-email">Email</label>
              <input
                id="login-email"
                type="email"
                value={email}
                placeholder="you@example.com"
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>
            <div className="form-group">
              <label htmlFor="login-password">Password</label>
              <input
                id="login-password"
                type="password"
                value={password}
                placeholder="••••••••"
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>
            {error && <p className="error">{error}</p>}
            <button type="submit" disabled={loading}>
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
          <p>No account? <Link to="/register">Create one</Link></p>
        </div>
      </div>
    </div>
  );
}
