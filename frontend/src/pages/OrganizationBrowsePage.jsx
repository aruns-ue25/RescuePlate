import React from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { 
  HeartHandshake, 
  User, 
  ArrowRight, 
  ShieldCheck, 
  Sparkles,
  Layers
} from 'lucide-react';

export default function OrganizationBrowsePage() {
  const { currentUser } = useAuth();

  return (
    <div className="browse-food-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner org-theme-banner">
        <div className="container">
          <div className="dashboard-header-flex">
            <div>
              <div className="badge badge-amber">
                <HeartHandshake size={14} />
                <span>Charity Portal</span>
              </div>
              <h1 className="dashboard-title">
                {currentUser?.businessName || "Charity Organization Portal"}
              </h1>
              <p className="dashboard-subtitle">
                Access portal for verified community shelters and hunger relief charities.
              </p>
            </div>

            {currentUser ? (
              <Link to="/profile" className="btn btn-amber btn-lg">
                <User size={18} />
                <span>View Charity Profile</span>
              </Link>
            ) : (
              <Link to="/register" className="btn btn-amber btn-lg">
                <span>Register Your Charity</span>
                <ArrowRight size={18} />
              </Link>
            )}
          </div>
        </div>
      </div>

      <div className="container dashboard-body">
        {/* Status Card */}
        <div className="dashboard-section-box" style={{ maxWidth: '850px', margin: '0 auto', textAlign: 'center', padding: '48px 32px' }}>
          <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
            <Sparkles size={32} className="text-amber" />
          </div>

          <h2 style={{ fontSize: '1.8rem', marginBottom: '12px' }}>Organization Onboarding Active</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', maxWidth: '600px', margin: '0 auto 32px' }}>
            Charity registration, accepted food preferences, and secure profile management are established in PostgreSQL.
            Live surplus donation browsing and partial claiming workflows will activate in the next release.
          </p>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px', marginBottom: '36px', textAlign: 'left' }}>
            <div style={{ background: 'var(--bg-alt)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border)' }}>
              <ShieldCheck size={20} className="text-amber" style={{ marginBottom: '8px' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: '700' }}>Organization Auth</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Role-based access verified</div>
            </div>

            <div style={{ background: 'var(--bg-alt)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border)' }}>
              <User size={20} className="text-amber" style={{ marginBottom: '8px' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: '700' }}>Preferences Stored</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Food categories configured</div>
            </div>

            <div style={{ background: 'var(--bg-alt)', padding: '20px', borderRadius: '12px', border: '1px solid var(--border)' }}>
              <Layers size={20} className="text-muted" style={{ marginBottom: '8px' }} />
              <div style={{ fontSize: '0.9rem', fontWeight: '700' }}>Marketplace Feed</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Ready for upcoming release</div>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'center', gap: '16px' }}>
            <Link to="/profile" className="btn btn-outline btn-lg">
              <span>Manage Profile Details</span>
            </Link>
            <Link to="/" className="btn btn-amber btn-lg">
              <span>Back to Home</span>
              <ArrowRight size={18} />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
