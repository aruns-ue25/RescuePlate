import React from 'react';
import { Mail, Phone, MapPin } from 'lucide-react';

export default function ContactSection() {
  return (
    <section className="contact-section" id="contact" style={{ padding: '60px 0 80px 0' }}>
      <div className="container" style={{ maxWidth: '650px', margin: '0 auto', textAlign: 'center' }}>
        <div className="badge badge-primary" style={{ marginBottom: '16px' }}>
          <Mail size={14} />
          <span>Get in Touch</span>
        </div>
        <h2 className="section-title" style={{ fontSize: '2rem', marginBottom: '16px' }}>Contact Support</h2>
        <p className="section-subtitle" style={{ marginBottom: '40px', color: 'var(--text-muted)' }}>
          For inquiries regarding account registration and platform onboarding, reach out to our team.
        </p>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', background: 'var(--bg-alt)', padding: '32px', borderRadius: '16px', border: '1px solid var(--border)', textAlign: 'left' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div style={{ width: '44px', height: '44px', borderRadius: '10px', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Mail size={20} className="text-emerald" />
            </div>
            <div>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Email Address</div>
              <strong style={{ fontSize: '1.05rem' }}>support@rescueplate.org</strong>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div style={{ width: '44px', height: '44px', borderRadius: '10px', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Phone size={20} className="text-amber" />
            </div>
            <div>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Phone Hotline</div>
              <strong style={{ fontSize: '1.05rem' }}>+94 (11) 234-5678</strong>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div style={{ width: '44px', height: '44px', borderRadius: '10px', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <MapPin size={20} className="text-emerald" />
            </div>
            <div>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Office Location</div>
              <strong style={{ fontSize: '1.05rem' }}>Colombo / Jaffna, Sri Lanka</strong>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
