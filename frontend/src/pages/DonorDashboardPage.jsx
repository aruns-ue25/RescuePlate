import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  Store, 
  Sparkles, 
  PlusCircle, 
  ArrowRight 
} from 'lucide-react';

export default function DonorDashboardPage() {
  const { currentUser } = useAuth();

  return (
    <div className="dashboard-page animate-fade-in-up">
      {/* Top Header */}
      <div className="dashboard-header-banner donor-theme-banner">
        <div className="container">
          <div className="badge badge-primary">
            <Store size={14} />
            <span>Food Donor Portal</span>
          </div>
          <h1 className="dashboard-title">
            {currentUser?.businessName ? `${currentUser.businessName} Portal` : "Post Surplus Food Portal"}
          </h1>
          <p className="dashboard-subtitle">
            Manage your surplus food listings, coordinate with local charities, and prevent commercial food waste.
          </p>
        </div>
      </div>

      <div className="container dashboard-body" style={{ maxWidth: '820px', margin: '40px auto 80px' }}>
        {/* Coming Soon / Next Sprint Message Card */}
        <div className="profile-card text-center" style={{ padding: '48px 32px' }}>
          <div 
            style={{ 
              width: '72px', 
              height: '72px', 
              borderRadius: '50%', 
              background: 'var(--emerald-light)', 
              color: 'var(--emerald)', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center', 
              margin: '0 auto 20px' 
            }}
          >
            <PlusCircle size={36} />
          </div>

          <div className="badge badge-primary" style={{ marginBottom: '14px' }}>
            <Sparkles size={13} />
            <span>Upcoming Feature • Sprint 2</span>
          </div>

          <h2 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '12px', color: 'var(--text-main)' }}>
            Surplus Food Posting & Donation Management
          </h2>

          <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', maxWidth: '600px', margin: '0 auto 36px' }}>
            The food surplus posting, portion management, expiry scheduling, and charity handover workflows are currently under development for our upcoming sprint.
          </p>

          <div style={{ display: 'flex', justifyContent: 'center', gap: '14px', flexWrap: 'wrap' }}>
            <Link to="/" className="btn btn-outline btn-md">
              <span>Return to Home</span>
            </Link>
            {currentUser ? (
              <Link to="/profile" className="btn btn-primary btn-md">
                <span>Manage Your Profile</span>
                <ArrowRight size={16} />
              </Link>
            ) : (
              <Link to="/register" className="btn btn-primary btn-md">
                <span>Register Donor Account</span>
                <ArrowRight size={16} />
              </Link>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
