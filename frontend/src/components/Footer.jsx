import React from 'react';
import { Link } from 'react-router-dom';
import { Utensils, Mail, Globe, ArrowRight } from 'lucide-react';

export default function Footer() {
  return (
    <footer className="site-footer">
      {/* Top Pre-Footer CTA Bar */}
      <div className="pre-footer-cta">
        <div className="container">
          <div className="pre-footer-content">
            <div className="pre-footer-text">
              <h3>Start Preventing Food Waste Today</h3>
              <p>Join commercial restaurants, hotels, and charities redistributing quality surplus meals.</p>
            </div>
            <div className="pre-footer-btns">
              <Link to="/donor-portal" className="btn btn-primary btn-lg">
                <span>Donate Surplus Food</span>
                <ArrowRight size={18} />
              </Link>
              <Link to="/browse-food" className="btn btn-dark-outline btn-lg">
                <span>Claim Food for Charity</span>
              </Link>
            </div>
          </div>
        </div>
      </div>

      <div className="container footer-main-content">
        <div className="footer-columns-grid" style={{ gridTemplateColumns: '2fr 1fr 1fr' }}>
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
          </div>

          {/* Column 2: Platform Links */}
          <div className="footer-col">
            <h4 className="footer-heading">Platform</h4>
            <ul className="footer-links-list">
              <li><Link to="/">Home</Link></li>
              <li><Link to="/browse-food">Browse Food</Link></li>
              <li><Link to="/donor-portal">Donor Portal</Link></li>
              <li><Link to="/how-it-works">How It Works</Link></li>
              <li><Link to="/about">About Us</Link></li>
              <li><Link to="/contact">Contact Support</Link></li>
            </ul>
          </div>

          {/* Column 3: Account & Access */}
          <div className="footer-col">
            <h4 className="footer-heading">Account & Access</h4>
            <ul className="footer-links-list">
              <li><Link to="/login">Sign In</Link></li>
              <li><Link to="/register">Register (Donor / Charity)</Link></li>
              <li><Link to="/profile">User Profile</Link></li>
            </ul>
          </div>
        </div>

        {/* Bottom Copyright Strip */}
        <div className="footer-bottom-bar">
          <div className="copyright-text">
            © {new Date().getFullYear()} RescuePlate – Food Waste Redistribution Platform. All rights reserved.
          </div>
        </div>
      </div>
    </footer>
  );
}
