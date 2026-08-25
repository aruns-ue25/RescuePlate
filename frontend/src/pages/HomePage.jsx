import React from 'react';
import { Link } from 'react-router-dom';
import HeroSection from '../components/HeroSection';
import ImpactStats from '../components/ImpactStats';
import FeaturesSection from '../components/FeaturesSection';
import CategoriesSection from '../components/CategoriesSection';
import TestimonialsSection from '../components/TestimonialsSection';
import { ArrowRight, Store, HeartHandshake } from 'lucide-react';

export default function HomePage() {
  return (
    <div className="home-page animate-fade-in-up">
      {/* Hero Section */}
      <HeroSection />

      {/* Live Impact Statistics */}
      <ImpactStats />

      {/* Quick Action Portals Banner */}
      <section className="portal-cards-section">
        <div className="container">
          <div className="portal-cards-grid">
            <div className="portal-card donor-portal-card">
              <div className="portal-icon-bubble bg-emerald-light">
                <Store size={32} className="text-emerald" />
              </div>
              <div className="portal-card-body">
                <span className="badge badge-primary">For Food Businesses</span>
                <h3>Are You a Restaurant, Bakery, or Hotel?</h3>
                <p>Don't throw away end-of-day surplus. List fresh surplus in under 60 seconds and support verified neighborhood shelters.</p>
                <Link to="/donor-portal" className="btn btn-primary btn-lg">
                  <span>Open Donor Portal</span>
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
                <p>Browse live surplus listings near you, claim the exact portion quantities you need, and arrange quick pickup or delivery.</p>
                <Link to="/browse-food" className="btn btn-amber btn-lg">
                  <span>Browse Available Food</span>
                  <ArrowRight size={18} />
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Core Features Grid */}
      <FeaturesSection />

      {/* Food Categories */}
      <CategoriesSection />

      {/* Community Testimonials */}
      <TestimonialsSection />
    </div>
  );
}
