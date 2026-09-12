import React from 'react';
import AboutSection from '../components/AboutSection';
import { Globe } from 'lucide-react';

export default function AboutPage() {
  return (
    <div className="page-view animate-fade-in-up">
      <div className="page-hero-banner">
        <div className="container text-center">
          <div className="badge badge-primary">
            <Globe size={14} />
            <span>Our Origin & Purpose</span>
          </div>
          <h1 className="page-hero-title">About RescuePlate</h1>
          <p className="page-hero-subtitle">
            Pioneering a zero-waste ecosystem connecting commercial food businesses with frontline hunger relief charities.
          </p>
        </div>
      </div>

      <AboutSection />
    </div>
  );
}
