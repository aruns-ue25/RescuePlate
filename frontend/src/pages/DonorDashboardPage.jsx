import React from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { 
  Store, 
  HeartHandshake,
  User, 
  ArrowRight, 
  Sparkles,
  Lock
} from 'lucide-react';

export default function DonorDashboardPage() {
  const { currentUser } = useAuth();

  const isDonor = currentUser?.role === 'DONOR';
  const isCharity = currentUser?.role === 'ORGANIZATION';

  return (
    <div className="dashboard-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner donor-theme-banner" style={{ padding: '60px 0 50px 0' }}>
        <div className="container">
          <div className="dashboard-header-flex">
            <div>
              <div className="badge badge-primary" style={{ marginBottom: '12px' }}>
                <Store size={14} />
                <span>Food Donor Portal</span>
              </div>
              <h1 className="dashboard-title" style={{ fontSize: '2.4rem', color: '#ffffff', marginBottom: '8px' }}>
                {isDonor
                  ? (currentUser.businessName || "Welcome, Food Donor")
                  : "Post Surplus Food"}
              </h1>
              <p className="dashboard-subtitle" style={{ color: 'rgba(255,255,255,0.85)', fontSize: '1.05rem' }}>
                {isDonor
                  ? `Logged in as ${currentUser.email}. Authenticated donor session active.`
                  : isCharity
                  ? `Logged in as ${currentUser.email} (Charity Organization).`
                  : "Platform onboarding portal for restaurants, bakeries, and food businesses."}
              </p>
            </div>

            {isDonor && (
              <Link to="/profile" className="btn btn-primary btn-lg">
                <User size={18} />
                <span>Manage Donor Profile</span>
              </Link>
            )}

            {isCharity && (
              <Link to="/browse-food" className="btn btn-amber btn-lg">
                <HeartHandshake size={18} />
                <span>Go to Charity Portal</span>
              </Link>
            )}
          </div>
        </div>
      </div>

      <div className="container dashboard-body">
        {/* ========================================================
           CASE 1: Logged in as Verified DONOR
           ======================================================== */}
        {isDonor && (
          <div className="dashboard-section-box" style={{ maxWidth: '750px', margin: '40px auto 80px auto', textAlign: 'center', padding: '48px 32px' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <Sparkles size={32} className="text-emerald" />
            </div>

            <h2 style={{ fontSize: '1.8rem', marginBottom: '12px' }}>Donor Account Active</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', maxWidth: '580px', margin: '0 auto 32px' }}>
              Your business account is verified and active on the RescuePlate redistribution network.
              You can view or update your business profile details anytime.
            </p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', flexWrap: 'wrap' }}>
              <Link to="/profile" className="btn btn-outline btn-lg">
                <span>View & Edit My Profile</span>
              </Link>
              <Link to="/" className="btn btn-primary btn-lg">
                <span>Back to Home</span>
                <ArrowRight size={18} />
              </Link>
            </div>
          </div>
        )}

        {/* ========================================================
           CASE 2: Logged in as CHARITY (Role Notice & Guidance)
           ======================================================== */}
        {isCharity && (
          <div className="dashboard-section-box" style={{ maxWidth: '750px', margin: '40px auto 80px auto', textAlign: 'center', padding: '48px 32px', border: '1px solid #fde68a', background: 'var(--bg-alt)' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <HeartHandshake size={32} className="text-amber" />
            </div>

            <h2 style={{ fontSize: '1.8rem', marginBottom: '12px' }}>Charity Session Detected</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', maxWidth: '580px', margin: '0 auto 32px' }}>
              You are currently logged in as a <strong>Charity Organization</strong>. The Post Surplus portal is reserved for commercial Food Donors (Restaurants, Hotels, Bakeries). To browse available surplus food, please switch to your Charity Portal.
            </p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', flexWrap: 'wrap' }}>
              <Link to="/browse-food" className="btn btn-amber btn-lg">
                <HeartHandshake size={18} />
                <span>Go to Charity Portal</span>
              </Link>
              <Link to="/profile" className="btn btn-outline btn-lg">
                <span>View My Profile</span>
              </Link>
            </div>
          </div>
        )}

        {/* ========================================================
           CASE 3: GUEST (Not Logged In)
           ======================================================== */}
        {!currentUser && (
          <div style={{ maxWidth: '650px', margin: '40px auto 80px auto', textAlign: 'center', padding: '48px 32px', background: 'var(--bg-alt)', borderRadius: '20px', border: '1px solid var(--border)' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <Lock size={30} className="text-emerald" />
            </div>

            <h2 style={{ fontSize: '1.9rem', marginBottom: '14px' }}>Food Donor Authentication Required</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', marginBottom: '32px' }}>
              To post commercial surplus food and track food waste diversion, 
              please sign in with your verified donor credentials or register a new business account.
            </p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', flexWrap: 'wrap' }}>
              <Link to="/login" className="btn btn-outline btn-lg">
                <span>Sign In to Account</span>
              </Link>
              <Link to="/register" className="btn btn-primary btn-lg">
                <span>Register Food Business</span>
                <ArrowRight size={18} />
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
