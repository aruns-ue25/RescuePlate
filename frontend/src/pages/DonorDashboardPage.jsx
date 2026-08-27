import React from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { 
  Store, 
  User, 
  ArrowRight, 
  Clock, 
  ShieldCheck,
  Sparkles
} from 'lucide-react';

export default function DonorDashboardPage() {
  const { currentUser } = useAuth();

  return (
    <div className="dashboard-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner donor-theme-banner">
        <div className="container">
          <div className="dashboard-header-flex">
            <div>
              <div className="badge badge-primary">
                <Store size={14} />
                <span>Food Donor Portal</span>
              </div>
              <h1 className="dashboard-title">
                {currentUser?.businessName || "Welcome, Food Donor"}
              </h1>
              <p className="dashboard-subtitle">
                Logged in as <strong>{currentUser?.email}</strong>. Authenticated donor session active.
              </p>
            </div>

            <Link to="/profile" className="btn btn-primary btn-lg">
              <User size={18} />
              <span>Manage Donor Profile</span>
            </Link>
          </div>
        </div>
      </div>

      <div className="container dashboard-body">
        {/* Status Card */}
        <div className="dashboard-section-box" style={{ maxWidth: '850px', margin: '0 auto', textAlign: 'center', padding: '48px 32px' }}>
          <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
            <Sparkles size={32} className="text-emerald" />
          </div>

          <h2 style={{ fontSize: '1.8rem', marginBottom: '12px' }}>Donor Account Active</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', maxWidth: '600px', margin: '0 auto 32px' }}>
            Your business account is verified and securely connected to PostgreSQL.
            Surplus food posting and dynamic inventory management modules will activate in the next release.
          </p>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px', marginBottom: '36px', textAlign: 'left' }}>
            <div style={{ background: 'var(--bg-alt)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border)' }}>
              <ShieldCheck size={20} className="text-emerald" style={{ marginBottom: '8px' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: '700' }}>Authentication</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Role-based JWT session verified</div>
            </div>

            <div style={{ background: 'var(--bg-alt)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border)' }}>
              <User size={20} className="text-emerald" style={{ marginBottom: '8px' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: '700' }}>Donor Profile</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Business details stored in DB</div>
            </div>

            <div style={{ background: 'var(--bg-alt)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border)' }}>
              <Clock size={20} className="text-amber" style={{ marginBottom: '8px' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: '700' }}>Surplus Posting</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Scheduled for upcoming release</div>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'center', gap: '16px' }}>
            <Link to="/profile" className="btn btn-outline btn-lg">
              <span>View & Edit My Profile</span>
            </Link>
            <Link to="/" className="btn btn-primary btn-lg">
              <span>Back to Home</span>
              <ArrowRight size={18} />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
