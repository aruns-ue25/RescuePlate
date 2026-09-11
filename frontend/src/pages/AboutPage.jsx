import React from 'react';
import AboutSection from '../components/AboutSection';
import { Globe, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

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

      {/* Leadership & Engineering Section */}
      <section className="team-section">
        <div className="container">
          <div className="section-header text-center">
            <h2 className="section-title">Developed For Community Impact</h2>
            <p className="section-subtitle">
              Engineered with modern architecture, ASP.NET Core, PostgreSQL, and React.
            </p>
          </div>

          <div className="team-grid">
            {[
              { name: "Sugarthan Arun", role: "Software Architect & Backend Lead" },
              { name: "Kathisan A.M.", role: "Full-Stack Engineer & Database Specialist" },
              { name: "Srikarsan K.", role: "Frontend & UI/UX Developer" },
              { name: "Sathushan M.", role: "QA & Integration Engineer" }
            ].map((author, i) => (
              <div key={i} className="team-card">
                <div className="team-avatar">👨‍💻</div>
                <h4 className="team-name">{author.name}</h4>
                <p className="team-role">{author.role}</p>
              </div>
            ))}
          </div>

          <div className="about-cta-bar text-center">
            <h3>Ready to Join the Movement?</h3>
            <p>Whether you have surplus meals to donate or represent a local community shelter, we're ready to onboard you.</p>
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
