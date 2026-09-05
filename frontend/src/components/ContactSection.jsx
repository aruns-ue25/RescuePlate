import React from 'react';
import { Mail, Phone, MapPin, Clock, MessageSquare } from 'lucide-react';

export default function ContactSection() {
  return (
    <section className="contact-section" id="contact">
      <div className="container">
        <div className="section-header text-center">
          <div className="badge badge-primary">
            <Mail size={14} />
            <span>Support & Inquiries</span>
          </div>
          <h2 className="section-title">Get in Touch with RescuePlate</h2>
          <p className="section-subtitle">
            Have questions about registering your business or organization? Reach out to our community coordinators.
          </p>
        </div>

        <div className="contact-grid" style={{ gridTemplateColumns: '1fr 1fr', maxWidth: '900px', margin: '0 auto' }}>
          {/* Contact Details Card */}
          <div className="contact-form-card">
            <h3 className="form-card-title">Contact Channels</h3>
            <p className="form-card-subtitle">Official communication channels for partners and community groups.</p>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '20px', marginTop: '24px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div style={{ width: '42px', height: '42px', borderRadius: '10px', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Mail size={20} className="text-emerald" />
                </div>
                <div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Email Support</div>
                  <strong style={{ fontSize: '1rem' }}>support@rescueplate.org</strong>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div style={{ width: '42px', height: '42px', borderRadius: '10px', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Phone size={20} className="text-amber" />
                </div>
                <div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Phone Helpline</div>
                  <strong style={{ fontSize: '1rem' }}>+94 (11) 234-5678</strong>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div style={{ width: '42px', height: '42px', borderRadius: '10px', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <MapPin size={20} className="text-emerald" />
                </div>
                <div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Headquarters</div>
                  <strong style={{ fontSize: '1rem' }}>Colombo / Jaffna, Sri Lanka</strong>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div style={{ width: '42px', height: '42px', borderRadius: '10px', background: 'var(--bg-alt)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Clock size={20} className="text-muted" />
                </div>
                <div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Operating Hours</div>
                  <strong style={{ fontSize: '1rem' }}>Mon - Sat: 8:00 AM - 8:00 PM</strong>
                </div>
              </div>
            </div>
          </div>

          {/* Quick Inquiry Note */}
          <div className="contact-form-card" style={{ background: '#ffffff', border: '1.5px solid var(--border)' }}>
            <h3 className="form-card-title">Send a Quick Message</h3>
            <p className="form-card-subtitle">Leave a message for our community partner onboarding team.</p>

            <form onSubmit={(e) => { e.preventDefault(); alert('Inquiry submitted! Our team will respond shortly.'); }} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div className="form-group">
                <label className="form-label">Your Name</label>
                <input type="text" className="form-input" placeholder="e.g. Kathisan" required />
              </div>

              <div className="form-group">
                <label className="form-label">Your Email</label>
                <input type="email" className="form-input" placeholder="name@domain.com" required />
              </div>

              <div className="form-group">
                <label className="form-label">Message</label>
                <textarea rows={3} className="form-input form-textarea" placeholder="How can we help you?" required></textarea>
              </div>

              <button type="submit" className="btn btn-primary full-width">
                <MessageSquare size={16} />
                <span>Submit Inquiry</span>
              </button>
            </form>
          </div>
        </div>
      </div>
    </section>
  );
}
