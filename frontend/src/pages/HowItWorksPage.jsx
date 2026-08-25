import React from 'react';
import HowItWorks from '../components/HowItWorks';
import { HelpCircle } from 'lucide-react';

export default function HowItWorksPage() {
  return (
    <div className="page-view animate-fade-in-up">
      <div className="page-hero-banner">
        <div className="container text-center">
          <div className="badge badge-amber">
            <HelpCircle size={14} />
            <span>End-to-End Workflow Guide</span>
          </div>
          <h1 className="page-hero-title">How Food Redistribution Works</h1>
          <p className="page-hero-subtitle">
            A simple step-by-step breakdown of how surplus food is posted, claimed by charities, and collected safely.
          </p>
        </div>
      </div>

      <HowItWorks />
    </div>
  );
}
