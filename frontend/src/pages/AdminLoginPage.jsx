import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function AdminLoginPage() {
  const navigate = useNavigate();
  const { adminLogin, verifyAdminAccessKey } = useAuth();

  // Stage 1: Email & Password
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  // Stage 2: Access Key PIN Challenge
  const [stage, setStage] = useState(1); // 1 = Credentials, 2 = Access Key PIN
  const [challengeToken, setChallengeToken] = useState('');
  const [accessKey, setAccessKey] = useState('');
  const [timeLeft, setTimeLeft] = useState(300); // 5 minutes

  // UI status states
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Timer effect for challenge expiry
  useEffect(() => {
    if (stage !== 2) return;

    const timer = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          setStage(1);
          setChallengeToken('');
          setError('Pre-authentication challenge expired. Please enter your credentials again.');
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [stage]);

  const handleStage1Submit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const res = await adminLogin(email, password);
      if (res.success && res.data?.challengeToken) {
        setChallengeToken(res.data.challengeToken);
        setTimeLeft(res.data.expiresInSeconds || 300);
        setStage(2);
      } else {
        setError(res.message || 'Administrator authentication failed.');
      }
    } catch (err) {
      setError(err.message || 'Invalid administrator email or password.');
    } finally {
      setLoading(false);
    }
  };

  const handleStage2Submit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const res = await verifyAdminAccessKey(challengeToken, accessKey);
      if (res.success) {
        // Successfully verified admin access
        navigate('/admin/dashboard');
      } else {
        setError(res.message || 'Access key verification failed.');
      }
    } catch (err) {
      setError(err.message || 'Invalid Administrator Access Key.');
    } finally {
      setLoading(false);
    }
  };

  const formatTimer = (seconds) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  };

  return (
    <div className="admin-login-wrapper" style={{
      minHeight: '85vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)',
      color: '#f8fafc',
      padding: '2rem'
    }}>
      <div className="admin-login-card" style={{
        maxWidth: '460rem',
        width: '100%',
        backgroundColor: 'rgba(30, 41, 59, 0.85)',
        backdropFilter: 'blur(12px)',
        border: '1px solid rgba(255, 255, 255, 0.1)',
        borderRadius: '16px',
        padding: '2.5rem',
        boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)'
      }}>
        {/* Header */}
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: '64px',
            height: '64px',
            borderRadius: '50%',
            background: 'linear-gradient(135deg, #ef4444 0%, #b91c1c 100%)',
            marginBottom: '1rem',
            boxShadow: '0 0 20px rgba(239, 68, 68, 0.4)'
          }}>
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <h2 style={{ fontSize: '1.75rem', fontWeight: '700', color: '#ffffff', margin: 0 }}>
            RescuePlate Admin Console
          </h2>
          <p style={{ color: '#94a3b8', fontSize: '0.9rem', marginTop: '0.5rem' }}>
            Protected Administrator Control Portal
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div style={{
            backgroundColor: 'rgba(239, 68, 68, 0.15)',
            borderLeft: '4px solid #ef4444',
            padding: '0.85rem 1rem',
            borderRadius: '6px',
            color: '#fca5a5',
            fontSize: '0.9rem',
            marginBottom: '1.5rem'
          }}>
            {error}
          </div>
        )}

        {/* Stage 1: Email & Password */}
        {stage === 1 && (
          <form onSubmit={handleStage1Submit}>
            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: '600', color: '#cbd5e1', marginBottom: '0.5rem' }}>
                Administrator Email Address
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="admin@rescueplate.org"
                style={{
                  width: '100%',
                  padding: '0.8rem 1rem',
                  backgroundColor: '#0f172a',
                  border: '1px solid #334155',
                  borderRadius: '8px',
                  color: '#ffffff',
                  fontSize: '0.95rem',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
            </div>

            <div style={{ marginBottom: '1.75rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: '600', color: '#cbd5e1', marginBottom: '0.5rem' }}>
                Administrator Password
              </label>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••••••"
                style={{
                  width: '100%',
                  padding: '0.8rem 1rem',
                  backgroundColor: '#0f172a',
                  border: '1px solid #334155',
                  borderRadius: '8px',
                  color: '#ffffff',
                  fontSize: '0.95rem',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              style={{
                width: '100%',
                padding: '0.9rem',
                backgroundColor: '#ef4444',
                color: '#ffffff',
                border: 'none',
                borderRadius: '8px',
                fontSize: '1rem',
                fontWeight: '600',
                cursor: loading ? 'not-allowed' : 'pointer',
                transition: 'all 0.2s ease',
                opacity: loading ? 0.7 : 1,
                boxShadow: '0 4px 14px rgba(239, 68, 68, 0.4)'
              }}
            >
              {loading ? 'Authenticating Credentials...' : 'Authenticate Credentials →'}
            </button>
          </form>
        )}

        {/* Stage 2: Access Key PIN Verification */}
        {stage === 2 && (
          <form onSubmit={handleStage2Submit}>
            <div style={{
              backgroundColor: 'rgba(59, 130, 246, 0.1)',
              border: '1px solid rgba(59, 130, 246, 0.3)',
              borderRadius: '8px',
              padding: '0.85rem 1rem',
              marginBottom: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between'
            }}>
              <div>
                <span style={{ display: 'block', fontSize: '0.8rem', color: '#93c5fd', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Step 2 Verification Active
                </span>
                <span style={{ fontSize: '0.85rem', color: '#e0f2fe' }}>
                  Authenticated as <strong>{email}</strong>
                </span>
              </div>
              <div style={{
                backgroundColor: '#1e3a8a',
                color: '#93c5fd',
                padding: '0.35rem 0.65rem',
                borderRadius: '20px',
                fontSize: '0.8rem',
                fontWeight: '700'
              }}>
                ⏱ {formatTimer(timeLeft)}
              </div>
            </div>

            <div style={{ marginBottom: '1.75rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: '600', color: '#cbd5e1', marginBottom: '0.5rem' }}>
                Administrator Access Key / PIN
              </label>
              <input
                type="password"
                required
                autoFocus
                value={accessKey}
                onChange={(e) => setAccessKey(e.target.value)}
                placeholder="Enter Secure Access Key (e.g. ADMIN-SECURE-KEY-2026)"
                style={{
                  width: '100%',
                  padding: '0.85rem 1rem',
                  backgroundColor: '#0f172a',
                  border: '2px solid #ef4444',
                  borderRadius: '8px',
                  color: '#ffffff',
                  fontSize: '1rem',
                  letterSpacing: '1px',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
              <span style={{ display: 'block', fontSize: '0.75rem', color: '#94a3b8', marginTop: '0.4rem' }}>
                Verification token expires in 5 minutes. Enforces global lockout after 5 failed attempts.
              </span>
            </div>

            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button
                type="button"
                onClick={() => {
                  setStage(1);
                  setAccessKey('');
                  setError('');
                }}
                style={{
                  flex: '1',
                  padding: '0.85rem',
                  backgroundColor: '#334155',
                  color: '#cbd5e1',
                  border: 'none',
                  borderRadius: '8px',
                  fontWeight: '600',
                  cursor: 'pointer'
                }}
              >
                ← Back
              </button>
              <button
                type="submit"
                disabled={loading}
                style={{
                  flex: '2',
                  padding: '0.85rem',
                  backgroundColor: '#10b981',
                  color: '#ffffff',
                  border: 'none',
                  borderRadius: '8px',
                  fontSize: '1rem',
                  fontWeight: '600',
                  cursor: loading ? 'not-allowed' : 'pointer',
                  boxShadow: '0 4px 14px rgba(16, 185, 129, 0.4)'
                }}
              >
                {loading ? 'Verifying PIN...' : 'Verify Access Key ✓'}
              </button>
            </div>
          </form>
        )}

        {/* Footer info */}
        <div style={{ marginTop: '2rem', textAlign: 'center', borderTop: '1px solid rgba(255, 255, 255, 0.08)', paddingTop: '1.25rem' }}>
          <Link to="/" style={{ color: '#94a3b8', fontSize: '0.85rem', textDecoration: 'none' }}>
            ← Return to Public Home Page
          </Link>
        </div>
      </div>
    </div>
  );
}
