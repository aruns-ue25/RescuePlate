import React from 'react';
import ContactSection from '../components/ContactSection';
import { Mail } from 'lucide-react';

export default function ContactPage() {
  return (
    <div className="page-view animate-fade-in-up">
      <div className="page-hero-banner">
        <div className="container text-center">
          <div className="badge badge-primary">
            <Mail size={14} />
            <span>Support & Inquiries</span>
          </div>
          <h1 className="page-hero-title">Contact RescuePlate</h1>
          <p className="page-hero-subtitle">
            Need assistance or have questions about partner onboarding? Reach out to our 24/7 team.
          </p>
        </div>
      </div>

      <ContactSection />
    </div>
  );
}
