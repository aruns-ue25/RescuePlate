import React from 'react';

export default function ContactPage() {
  return (
    <div className="page-view animate-fade-in-up" style={{ minHeight: '60vh', display: 'flex', alignItems: 'center', justifyContent: 'center', textAlign: 'center', padding: '120px 20px' }}>
      <div className="container" style={{ maxWidth: '600px' }}>
        <h1 style={{ fontSize: '2.4rem', marginBottom: '16px' }}>Contact Support</h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '1.1rem', lineHeight: '1.6' }}>
          Official Support Channel: <strong>support@rescueplate.org</strong> • Hotline: <strong>+94 (11) 234-5678</strong>
        </p>
      </div>
    </div>
  );
}
