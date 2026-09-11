import React from 'react';
import HowItWorks from '../components/HowItWorks';
import ImpactCalculator from '../components/ImpactCalculator';
import { HelpCircle, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

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
            A step-by-step breakdown of how surplus food is posted, claimed with partial fulfilment, and collected safely.
          </p>
        </div>
      </div>

      <HowItWorks />

      <ImpactCalculator />

      <div className="container" style={{ marginBottom: '80px' }}>
        <div className="workflow-callout-box">
          <div className="callout-text">
            <h4>Ready to get started?</h4>
            <p>Create your account in 60 seconds and begin saving surplus food today.</p>
          </div>
          <Link to="/register" className="btn btn-primary btn-lg">
            <span>Register Now</span>
            <ArrowRight size={18} />
          </Link>
        </div>
      </div>
    </div>
  );
}
