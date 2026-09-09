import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import HeroSection from '../components/HeroSection';
import ImpactStats from '../components/ImpactStats';
import FeaturesSection from '../components/FeaturesSection';
import CategoriesSection from '../components/CategoriesSection';
import { ArrowRight, Store, HeartHandshake } from 'lucide-react';

export default function HomePage() {
  const navigate = useNavigate();

  const handleOpenAuth = (mode, role) => {
    navigate('/register');
  };

  return (
    <div className="home-page animate-fade-in-up">
      {/* Hero Section */}
      <HeroSection />

      {/* Live Impact Telemetry Stats */}
      <ImpactStats />

      {/* Quick Action Portals */}
      <section className="portal-cards-section" style={{ padding: '60px 0 80px 0' }}>
        <div className="container">
          <div className="portal-cards-grid">
            <div className="portal-card donor-portal-card">
              <div className="portal-icon-bubble bg-emerald-light">
                <Store size={32} className="text-emerald" />
              </div>
              <div className="portal-card-body">
                <span className="badge badge-primary">For Food Businesses</span>
                <h3>Are You a Restaurant, Bakery, or Hotel?</h3>
                <p>Register your food business to create your donor profile and get ready for surplus redistribution.</p>
                <Link to="/register" className="btn btn-primary btn-lg">
                  <span>Register as Food Donor</span>
                  <ArrowRight size={18} />
                </Link>
              </div>
            </div>

            <div className="portal-card org-portal-card">
              <div className="portal-icon-bubble bg-amber-light">
                <HeartHandshake size={32} className="text-amber" />
              </div>
              <div className="portal-card-body">
                <span className="badge badge-amber">For Charities & Shelters</span>
                <h3>Need Fresh Surplus Food for Your Community?</h3>
                <p>Register your certified nonprofit organization or shelter to set your accepted food preferences.</p>
                <Link to="/register" className="btn btn-amber btn-lg">
                  <span>Register as Charity</span>
                  <ArrowRight size={18} />
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Platform Reliability & Safety Features */}
      <FeaturesSection />

      {/* Accepted Food Categories Grid */}
      <CategoriesSection onOpenAuth={handleOpenAuth} />
    </div>
  );
}
