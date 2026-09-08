import React from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { 
  HeartHandshake, 
  User, 
  Sparkles, 
  MapPin, 
  Clock, 
  Utensils, 
  Croissant, 
  Apple, 
  ArrowRight, 
  ShieldCheck, 
  CheckCircle2,
  Lock
} from 'lucide-react';

export default function OrganizationBrowsePage() {
  const { currentUser } = useAuth();

  const sampleListings = [
    {
      id: 1,
      title: "Artisan Sourdough & Croissants",
      donorName: "Sunrise Artisan Bakery",
      category: "Bakery & Pastries",
      icon: <Croissant size={20} className="text-amber" />,
      quantity: "15 Portions (12 kg)",
      location: "Colombo 03 / Central District",
      pickupWindow: "Available until 7:00 PM Today",
      badgeColor: "badge-amber"
    },
    {
      id: 2,
      title: "Hot Buffet Rice & Vegetable Curry",
      donorName: "Grand Azure Hotel & Banquet",
      category: "Cooked Meals",
      icon: <Utensils size={20} className="text-emerald" />,
      quantity: "35 Portions (Catering Trays)",
      location: "Kollupitiya, Colombo",
      pickupWindow: "Ready for Immediate Pickup",
      badgeColor: "badge-primary"
    },
    {
      id: 3,
      title: "Fresh Farm Organic Greens & Apples",
      donorName: "Metro Fresh Wholesalers",
      category: "Fresh Produce",
      icon: <Apple size={20} className="text-emerald" />,
      quantity: "25 kg (Crates)",
      location: "Pettah Wholesale Market",
      pickupWindow: "Available 24-Hour Window",
      badgeColor: "badge-primary"
    }
  ];

  return (
    <div className="browse-food-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner org-theme-banner" style={{ padding: '60px 0 50px 0' }}>
        <div className="container">
          <div className="dashboard-header-flex">
            <div>
              <div className="badge badge-amber" style={{ marginBottom: '12px' }}>
                <HeartHandshake size={14} />
                <span>{currentUser ? "Charity Discovery Portal" : "Public Food Browse"}</span>
              </div>
              <h1 className="dashboard-title" style={{ fontSize: '2.4rem', color: '#ffffff', marginBottom: '8px' }}>
                {currentUser 
                  ? `Welcome, ${currentUser.businessName || currentUser.contactName || "Charity Partner"}`
                  : "Browse Available Surplus Food"}
              </h1>
              <p className="dashboard-subtitle" style={{ color: 'rgba(255,255,255,0.85)', fontSize: '1.05rem' }}>
                {currentUser 
                  ? `Logged in as ${currentUser.email} • Verified Partner Organization.`
                  : "Connecting commercial food donors with certified hunger-relief charities."}
              </p>
            </div>

            {currentUser && (
              <Link to="/profile" className="btn btn-amber btn-lg">
                <User size={18} />
                <span>Manage Organization Profile</span>
              </Link>
            )}
          </div>
        </div>
      </div>

      <div className="container" style={{ padding: '50px 20px 80px 20px' }}>
        {currentUser ? (
          /* ========================================================
             LOGGED-IN VIEW: Active Charity Marketplace Preview
             ======================================================== */
          <div>
            {/* Status Info Bar */}
            <div style={{ 
              background: 'var(--bg-alt)', 
              border: '1px solid var(--border)', 
              borderRadius: '16px', 
              padding: '24px 28px', 
              marginBottom: '40px',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: '16px'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <ShieldCheck size={26} className="text-amber" />
                </div>
                <div>
                  <h3 style={{ fontSize: '1.15rem', marginBottom: '4px' }}>Verified Charity Organization Session</h3>
                  <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', margin: 0 }}>
                    Your organization preferences and contact records are active and up to date.
                  </p>
                </div>
              </div>

              <Link to="/profile" className="btn btn-outline" style={{ fontSize: '0.9rem' }}>
                <span>Edit Preferences</span>
                <ArrowRight size={14} />
              </Link>
            </div>

            {/* Available Surplus Feed Header */}
            <div style={{ marginBottom: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', flexWrap: 'wrap', gap: '12px' }}>
              <div>
                <h2 style={{ fontSize: '1.8rem', marginBottom: '6px' }}>Live Surplus Food Listings</h2>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.95rem' }}>
                  Fresh food batches available for claim by verified charity organizations.
                </p>
              </div>
              <span className="badge badge-primary">
                <Sparkles size={12} />
                <span>3 Active Batches Available</span>
              </span>
            </div>

            {/* Listings Grid */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '24px', marginBottom: '48px' }}>
              {sampleListings.map((item) => (
                <div 
                  key={item.id} 
                  style={{ 
                    background: 'var(--card-bg)', 
                    border: '1px solid var(--border)', 
                    borderRadius: '16px', 
                    padding: '28px',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'space-between',
                    transition: 'transform 0.2s, box-shadow 0.2s'
                  }}
                >
                  <div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
                      <span className={`badge ${item.badgeColor}`}>
                        {item.icon}
                        <span style={{ marginLeft: '6px' }}>{item.category}</span>
                      </span>
                      <span style={{ fontSize: '0.82rem', fontWeight: 600, color: 'var(--primary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                        <CheckCircle2 size={13} /> Verified Donor
                      </span>
                    </div>

                    <h3 style={{ fontSize: '1.25rem', marginBottom: '8px' }}>{item.title}</h3>
                    <div style={{ fontSize: '0.92rem', color: 'var(--text-muted)', marginBottom: '18px' }}>
                      Provided by: <strong style={{ color: 'var(--text-main)' }}>{item.donorName}</strong>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', fontSize: '0.88rem', color: 'var(--text-muted)', marginBottom: '24px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <span style={{ fontWeight: 600, color: 'var(--text-main)' }}>Quantity:</span> {item.quantity}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <MapPin size={15} className="text-amber" />
                        <span>{item.location}</span>
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <Clock size={15} className="text-emerald" />
                        <span>{item.pickupWindow}</span>
                      </div>
                    </div>
                  </div>

                  <div style={{ paddingTop: '16px', borderTop: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: '0.82rem', color: 'var(--text-muted)' }}>Redistribution Ready</span>
                    <Link to="/profile" className="btn btn-outline" style={{ fontSize: '0.85rem', padding: '6px 14px' }}>
                      <span>View Org Profile</span>
                    </Link>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : (
          /* ========================================================
             GUEST VIEW: Sign in / Register Callout
             ======================================================== */
          <div style={{ maxWidth: '650px', margin: '40px auto 80px auto', textAlign: 'center', padding: '48px 32px', background: 'var(--bg-alt)', borderRadius: '20px', border: '1px solid var(--border)' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--secondary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <Lock size={30} className="text-amber" />
            </div>

            <h2 style={{ fontSize: '1.9rem', marginBottom: '14px' }}>Charity Partner Authentication Required</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', lineHeight: '1.6', marginBottom: '32px' }}>
              To ensure food safety and direct delivery to authentic community shelters, 
              please sign in with your verified organization credentials or register a new charity account.
            </p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', flexWrap: 'wrap' }}>
              <Link to="/login" className="btn btn-outline btn-lg">
                <span>Sign In to Account</span>
              </Link>
              <Link to="/register" className="btn btn-amber btn-lg">
                <span>Register Charity Account</span>
                <ArrowRight size={18} />
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
