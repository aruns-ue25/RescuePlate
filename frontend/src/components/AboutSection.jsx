import React from 'react';
import { Globe, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function AboutSection() {
  return (
    <section className="about-section" id="about" style={{ padding: '60px 0' }}>
      <div className="container" style={{ maxWidth: '800px', textAlign: 'center' }}>
        <div className="badge badge-primary" style={{ marginBottom: '16px' }}>
          <Globe size={14} />
          <span>About RescuePlate</span>
        </div>
        <h2 className="about-title" style={{ fontSize: '2.2rem', marginBottom: '20px' }}>
          Connecting Surplus Food with Communities
        </h2>
        <p className="about-paragraph" style={{ fontSize: '1.1rem', color: 'var(--text-muted)', lineHeight: '1.7', marginBottom: '32px' }}>
          RescuePlate is an initiative to bridge the gap between commercial food donors (hotels, restaurants, bakeries) 
          and verified charity organizations. Create an account today to become part of the redistribution network.
        </p>
        <div style={{ display: 'flex', justifyContent: 'center', gap: '16px' }}>
          <Link to="/register" className="btn btn-primary btn-lg">
            <span>Register Account</span>
            <ArrowRight size={18} />
          </Link>
          <Link to="/login" className="btn btn-outline btn-lg">
            <span>Sign In</span>
          </Link>
        </div>
      </div>
    </section>
  );
}
