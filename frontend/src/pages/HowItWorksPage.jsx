import React from 'react';

export default function HowItWorksPage() {
  return (
    <div className="page-view animate-fade-in-up" style={{ minHeight: '60vh', display: 'flex', alignItems: 'center', justifyContent: 'center', textAlign: 'center', padding: '120px 20px' }}>
      <div className="container" style={{ maxWidth: '600px' }}>
        <h1 style={{ fontSize: '2.4rem', marginBottom: '16px' }}>How It Works</h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '1.1rem', lineHeight: '1.6' }}>
          Platform redistribution workflows and step-by-step donation guide coming soon.
        </p>
      </div>
    </div>
  );
}
