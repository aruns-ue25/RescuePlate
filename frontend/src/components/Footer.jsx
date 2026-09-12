import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Utensils, Mail, Globe, ArrowRight, CheckCircle2 } from 'lucide-react';

export default function Footer() {
  const [newsletterEmail, setNewsletterEmail] = useState('');
  const [subscribed, setSubscribed] = useState(false);

  const handleSubscribe = (e) => {
    e.preventDefault();
    if (!newsletterEmail) return;
    setSubscribed(true);
  };

  return (
    <footer className="site-footer">
      <div className="container footer-main-content">
        <div className="footer-columns-grid">
          {/* Brand Col */}
          <div className="footer-col brand-col">
            <div className="brand-logo footer-logo">
              <div className="logo-icon-wrapper">
                <Utensils className="logo-icon text-emerald" size={22} />
              </div>
              <div className="brand-text-group">
                <span className="brand-title text-white">Rescue<span className="brand-accent">Plate</span></span>
                <span className="brand-tagline">Food Waste Redistribution Platform</span>
              </div>
            </div>

            <p className="footer-about-text">
              RescuePlate is a dedicated web platform connecting commercial food surplus with charities 
              and community groups. Aligned with UN SDG 2 (Zero Hunger) & SDG 12 (Responsible Consumption).
            </p>

            <div className="tech-badge-strip">
              <span className="tech-pill">ASP.NET Web API</span>
              <span className="tech-pill">React.js</span>
              <span className="tech-pill">PostgreSQL</span>
              <span className="tech-pill">Kafka</span>
            </div>
          </div>

          {/* Column 2: Quick Links */}
          <div className="footer-col">
            <h4 className="footer-heading">Platform</h4>
            <ul className="footer-links-list">
              <li><Link to="/browse-food">Browse Surplus Food</Link></li>
              <li><Link to="/donor-portal">Donor Portal / Post Food</Link></li>
              <li><Link to="/how-it-works">How It Works</Link></li>
              <li><Link to="/about">About Us & Mission</Link></li>
              <li><Link to="/contact">Contact & Support</Link></li>
            </ul>
          </div>

          {/* Column 3: Portals & Roles */}
          <div className="footer-col">
            <h4 className="footer-heading">Account & Access</h4>
            <ul className="footer-links-list">
              <li><Link to="/login">Sign In to Dashboard</Link></li>
              <li><Link to="/register">Create New Account</Link></li>
              <li><Link to="/profile">My Account Profile</Link></li>
              <li><Link to="/how-it-works">Partial Fulfilment Guide</Link></li>
              <li><Link to="/contact">Food Safety Protections</Link></li>
            </ul>
          </div>

          {/* Column 4: Newsletter */}
          <div className="footer-col newsletter-col">
            <h4 className="footer-heading">Stay Informed</h4>
            <p className="newsletter-desc">
              Subscribe for monthly impact digests, zero-waste tips, and community rescue stories.
            </p>

            {subscribed ? (
              <div className="subscribed-badge">
                <CheckCircle2 size={16} className="text-emerald" />
                <span>You are subscribed to RescuePlate updates!</span>
              </div>
            ) : (
              <form onSubmit={handleSubscribe} className="newsletter-form">
                <input
                  type="email"
                  placeholder="Enter your email"
                  className="newsletter-input"
                  value={newsletterEmail}
                  onChange={(e) => setNewsletterEmail(e.target.value)}
                  required
                />
                <button type="submit" className="newsletter-btn" aria-label="Subscribe to newsletter">
                  <ArrowRight size={16} />
                </button>
              </form>
            )}

            <div className="social-links-row">
              <a href="https://github.com" target="_blank" rel="noreferrer" className="social-icon-btn" aria-label="GitHub">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4"></path><path d="M9 18c-4.51 2-5-2-7-2"></path></svg>
              </a>
              <a href="mailto:support@rescueplate.org" className="social-icon-btn" aria-label="Email">
                <Mail size={18} />
              </a>
              <a href="/" className="social-icon-btn" aria-label="Website">
                <Globe size={18} />
              </a>
            </div>
          </div>
        </div>

        {/* Bottom Copyright Strip */}
        <div className="footer-bottom-bar">
          <div className="copyright-text">
            © {new Date().getFullYear()} RescuePlate – Food Waste Redistribution Platform. All rights reserved.
          </div>
          <div className="authors-text">
            Authors: Sugarthan Arun, Kathisan A.M., Srikarsan K., Sathushan M.
          </div>
        </div>
      </div>
    </footer>
  );
}
