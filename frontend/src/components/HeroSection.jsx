import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { 
  ArrowRight, 
  Sparkles, 
  Clock, 
  MapPin, 
  CheckCircle2, 
  ShieldCheck, 
  Truck, 
  Building2, 
  Heart,
  TrendingUp,
  PackageCheck
} from 'lucide-react';

export default function HeroSection() {
  const [secondsRemaining, setSecondsRemaining] = useState(3600 * 3 + 24 * 60);

  useEffect(() => {
    const timer = setInterval(() => {
      setSecondsRemaining((prev) => (prev > 0 ? prev - 1 : 10800));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const formatTimer = (totalSeconds) => {
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  };

  return (
    <section className="hero-section" id="hero">
      {/* Background Decorative Gradient Orbs */}
      <div className="hero-blob hero-blob-1 animate-pulse-glow"></div>
      <div className="hero-blob hero-blob-2 animate-pulse-glow"></div>

      <div className="container hero-container">
        {/* Left Hero Content */}
        <div className="hero-content animate-fade-in-up">
          <div className="badge badge-primary hero-badge">
            <Sparkles size={14} />
            <span>UN SDG 2 (Zero Hunger) & SDG 12 Aligned</span>
          </div>

          <h1 className="hero-headline">
            Rescue Food. <br />
            <span className="text-gradient">Feed Hope.</span> <br />
            Zero Waste.
          </h1>

          <p className="hero-description">
            RescuePlate is the intelligent food redistribution platform connecting restaurants, 
            bakeries, hotels, and grocers directly with local charities and shelters. 
            Transform surplus meals into community impact in minutes.
          </p>

          {/* Action CTAs */}
          <div className="hero-cta-group">
            <Link
              to="/donor-portal"
              className="btn btn-primary btn-lg btn-glow hero-primary-btn"
            >
              <span>Donate Surplus Food</span>
              <ArrowRight size={18} />
            </Link>

            <Link
              to="/browse-food"
              className="btn btn-amber btn-lg hero-secondary-btn"
            >
              <Heart size={18} />
              <span>Claim Food for Charity</span>
            </Link>
          </div>

          {/* Quick Metrics Bar */}
          <div className="hero-trust-bar">
            <div className="trust-item">
              <ShieldCheck className="trust-icon" size={18} />
              <span>Verified Food Safety</span>
            </div>
            <div className="trust-divider"></div>
            <div className="trust-item">
              <TrendingUp className="trust-icon" size={18} />
              <span>Smart Partial Fulfilment</span>
            </div>
            <div className="trust-divider"></div>
            <div className="trust-item">
              <PackageCheck className="trust-icon" size={18} />
              <span>100% Free for Nonprofits</span>
            </div>
          </div>
        </div>

        {/* Right Hero Interactive Visual Card */}
        <div className="hero-visual-wrapper">
          <div className="live-donation-card animate-float">
            <div className="live-card-header">
              <div className="donor-pill">
                <Building2 size={16} className="text-emerald" />
                <div>
                  <div className="donor-name">Sunrise Artisan Bakery</div>
                  <div className="donor-location">
                    <MapPin size={12} /> Central Boulevard • 1.2 km away
                  </div>
                </div>
              </div>
              <span className="live-pulse-badge">
                <span className="pulse-dot"></span> LIVE LISTING
              </span>
            </div>

            <div className="live-food-info">
              <div className="food-thumb">🥐</div>
              <div className="food-details">
                <h3 className="food-title">Fresh Baked Pastries & Sourdough</h3>
                <p className="food-meta">Packaged fresh today at 4:00 PM</p>
                <div className="food-tags">
                  <span className="tag tag-green">Bakery & Grains</span>
                  <span className="tag tag-blue">Vegetarian</span>
                </div>
              </div>
            </div>

            {/* Quantity Progress Bar */}
            <div className="quantity-bar-group">
              <div className="quantity-labels">
                <span className="qty-title">Available Quantity</span>
                <span className="qty-numbers"><strong>65</strong> of 100 portions remaining</span>
              </div>
              <div className="progress-track">
                <div className="progress-fill" style={{ width: '65%' }}></div>
              </div>
              <div className="partial-claim-note">
                <CheckCircle2 size={13} className="text-emerald" />
                <span>35 portions already claimed by Hope Community Center</span>
              </div>
            </div>

            {/* Expiry Countdown & Pickup Tag */}
            <div className="live-card-footer">
              <div className="timer-badge" title="Time remaining before collection window closes">
                <Clock size={15} className="text-amber" />
                <span>Expires in <strong>{formatTimer(secondsRemaining)}</strong></span>
              </div>
              <div className="delivery-badge">
                <Truck size={14} />
                <span>Organization Pickup</span>
              </div>
            </div>
          </div>

          {/* Floating Badges */}
          <div className="floating-card floating-card-top animate-float-delayed">
            <div className="floating-icon-box bg-emerald-light">
              <Heart size={20} className="text-emerald" />
            </div>
            <div>
              <div className="floating-title">12,480+ Meals</div>
              <div className="floating-subtitle">Redistributed this month</div>
            </div>
          </div>

          <div className="floating-card floating-card-bottom animate-float">
            <div className="floating-icon-box bg-amber-light">
              <Sparkles size={20} className="text-amber" />
            </div>
            <div>
              <div className="floating-title">Zero Waste Hero</div>
              <div className="floating-subtitle">98.4% Claim rate within 2h</div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
