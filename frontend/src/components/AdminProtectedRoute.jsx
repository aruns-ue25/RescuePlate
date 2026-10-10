import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function AdminProtectedRoute({ children }) {
  const { currentUser } = useAuth();

  if (!currentUser) {
    return <Navigate to="/admin/login" replace />;
  }

  if (currentUser.role !== 'ADMIN') {
    return (
      <div style={{
        padding: '4rem 2rem',
        textAlign: 'center',
        backgroundColor: '#0f172a',
        color: '#f8fafc',
        minHeight: '70vh'
      }}>
        <div style={{
          maxWidth: '500px',
          margin: '0 auto',
          padding: '2.5rem',
          backgroundColor: '#1e293b',
          borderRadius: '16px',
          border: '1px solid #ef4444'
        }}>
          <h2 style={{ color: '#ef4444', marginBottom: '1rem' }}>403 — Administrative Access Denied</h2>
          <p style={{ color: '#94a3b8', marginBottom: '1.5rem' }}>
            Your account ({currentUser.email}) is registered as a <strong>{currentUser.role}</strong> and does not possess System Administrator privileges.
          </p>
          <a href="/" style={{
            display: 'inline-block',
            padding: '0.75rem 1.5rem',
            backgroundColor: '#3b82f6',
            color: '#ffffff',
            borderRadius: '8px',
            textDecoration: 'none',
            fontWeight: '600'
          }}>
            Return to Public Portal
          </a>
        </div>
      </div>
    );
  }

  return children;
}
