import React from 'react';
import AboutSection from '../components/AboutSection';
import { Globe, ArrowRight, ShieldCheck, HeartHandshake, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function AboutPage() {
  return (
    <div className="page-view animate-fade-in-up">
      <div className="page-hero-banner">
        <div className="container text-center">
          <div className="badge badge-primary">
            <Globe size={14} />
            <span>Our Purpose & Mission</span>
          </div>
          <h1 className="page-hero-title">About RescuePlate</h1>
          <p className="page-hero-subtitle">
            Pioneering a zero-waste ecosystem connecting commercial food businesses with frontline hunger relief charities.
          </p>
        </div>
      </div>

      <AboutSection />

      <section className="team-section" style={{ padding: '60px 0' }}>
        <div className="container">
          <div className="about-cta-bar text-center">
            <h3>Ready to Join the Movement?</h3>
            <p>Whether you have surplus meals to donate or represent a local community shelter, create your account today.</p>
            <div className="about-cta-btns">
              <Link to="/register" className="btn btn-primary btn-lg">
                <span>Join RescuePlate Today</span>
                <ArrowRight size={18} />
              </Link>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
