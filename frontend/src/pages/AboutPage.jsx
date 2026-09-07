import React from 'react';

export default function AboutPage() {
  return (
    <div className="page-view animate-fade-in-up" style={{ minHeight: '60vh', display: 'flex', alignItems: 'center', justifyContent: 'center', textAlign: 'center', padding: '120px 20px' }}>
      <div className="container" style={{ maxWidth: '600px' }}>
        <h1 style={{ fontSize: '2.4rem', marginBottom: '16px' }}>About RescuePlate</h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '1.1rem', lineHeight: '1.6' }}>
          Zero food waste redistribution platform connecting commercial food donors with community charities.
        </p>
      </div>
    </div>
  );
}
