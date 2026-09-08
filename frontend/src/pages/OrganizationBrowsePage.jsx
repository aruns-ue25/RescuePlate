import React from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { 
  HeartHandshake, 
  Store,
  User, 
  ArrowRight, 
  Sparkles,
  Lock
} from 'lucide-react';

export default function OrganizationBrowsePage() {
  const { currentUser } = useAuth();

  const isCharity = currentUser?.role === 'ORGANIZATION';
  const isDonor = currentUser?.role === 'DONOR';

  return (
    <div className="browse-food-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner org-theme-banner" style={{ padding: '60px 0 50px 0' }}>
        <div className="container">
          <div className="dashboard-header-flex">
            <div>
              <div className="badge badge-amber" style={{ marginBottom: '12px' }}>
                {isDonor ? <Store size={14} /> : <HeartHandshake size={14} />}
                <span>{isDonor ? "Community Surplus Browse" : "Charity Organization Portal"}</span>
              </div>
              <h1 className="dashboard-title" style={{ fontSize: '2.4rem', color: '#ffffff', marginBottom: '8px' }}>
                {currentUser 
                  ? (currentUser.businessName || currentUser.contactName || (isDonor ? "Welcome, Food Donor" : "Welcome, Charity Partner"))
                  : "Browse Surplus Food"}
              </h1>
              <p className="dashboard-subtitle" style={{ color: 'rgba(255,255,255,0.85)', fontSize: '1.05rem' }}>
                {currentUser 
                  ? `Logged in as ${currentUser.email}. Authenticated session active.`
                  : "Connecting commercial food donors with certified hunger-relief charities."}
              </p>
            </div>

            {currentUser && (
              <Link to="/profile" className="btn btn-amber btn-lg">
                <User size={18} />
                <span>Manage My Profile</span>
              </Link>
            )}
          </div>
        </div>
      </div>

      <div className="container dashboard-body">
        {currentUser ? (
          /* ========================================================
             LOGGED-IN VIEW (Accessible to all authenticated users)
             ======================================================== */
          <div className="dashboard-section-box" style={{ maxWidth: '750px', margin: '40px auto 80px auto', textAlign: 'center', padding: '48px 32px' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <Sparkles size={32} className="text-amber" />
            </div>

            <h2 style={{ fontSize: '1.8rem', marginBottom: '12px' }}>Surplus Food Marketplace</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', maxWidth: '580px', margin: '0 auto 32px' }}>
              Surplus food listings from verified commercial restaurants, bakeries, and hotels are distributed directly to partner charities and community shelters.
            </p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', flexWrap: 'wrap' }}>
              <Link to="/profile" className="btn btn-outline btn-lg">
                <span>View & Edit My Profile</span>
              </Link>
              <Link to="/" className="btn btn-amber btn-lg">
                <span>Back to Home</span>
                <ArrowRight size={18} />
              </Link>
            </div>
          </div>
        ) : (
          /* ========================================================
             GUEST VIEW (Not Logged In)
             ======================================================== */
          <div style={{ maxWidth: '650px', margin: '40px auto 80px auto', textAlign: 'center', padding: '48px 32px', background: 'var(--bg-alt)', borderRadius: '20px', border: '1px solid var(--border)' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <Lock size={30} className="text-amber" />
            </div>

            <h2 style={{ fontSize: '1.9rem', marginBottom: '14px' }}>Community Authentication Required</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', marginBottom: '32px' }}>
              To ensure food safety and direct delivery to authentic community shelters, 
              please sign in with your credentials or register a new account.
            </p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', flexWrap: 'wrap' }}>
              <Link to="/login" className="btn btn-outline btn-lg">
                <span>Sign In to Account</span>
              </Link>
              <Link to="/register" className="btn btn-amber btn-lg">
                <span>Register Account</span>
                <ArrowRight size={18} />
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
