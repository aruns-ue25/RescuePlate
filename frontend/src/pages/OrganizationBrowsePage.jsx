import React from 'react';
import { Link } from 'react-router-dom';

export default function OrganizationBrowsePage() {
  return (
    <div className="browse-food-page animate-fade-in-up" style={{ minHeight: '60vh', display: 'flex', alignItems: 'center', justifyContent: 'center', textAlign: 'center', padding: '120px 20px' }}>
      <div className="container" style={{ maxWidth: '600px' }}>
        <h1 style={{ fontSize: '2.4rem', marginBottom: '16px' }}>Browse Surplus Food</h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '1.1rem', lineHeight: '1.6', marginBottom: '24px' }}>
          Surplus food marketplace for verified charities will activate in upcoming release.
        </p>
        <Link to="/register" className="btn btn-primary">
          <span>Register Charity Account</span>
        </Link>
      </div>
    </div>
  );
}
