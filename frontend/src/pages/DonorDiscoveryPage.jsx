import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { donationApi, getProfileImageUrl } from '../services/api';
import { 
  Building2, 
  MapPin, 
  Search, 
  RefreshCw, 
  Package, 
  CheckCircle2, 
  AlertCircle, 
  ExternalLink, 
  Clock, 
  Tag, 
  X, 
  ChevronRight, 
  Sparkles, 
  ShieldCheck, 
  HeartHandshake,
  Users,
  Store,
  Send
} from 'lucide-react';

export default function DonorDiscoveryPage() {
  const { currentUser } = useAuth();

  const [donors, setDonors] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Search and Filter state
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState('ALL');

  // Selected Donor Modal state (Scenario 4)
  const [selectedDonor, setSelectedDonor] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailData, setDetailData] = useState(null);
  const [detailError, setDetailError] = useState(null);

  const donorTypes = [
    'ALL', 
    'Restaurant', 
    'Bakery', 
    'Supermarket', 
    'Hotel', 
    'Catering', 
    'Food Business'
  ];

  const fetchDonors = async (search = searchQuery, type = selectedType) => {
    try {
      setLoading(true);
      setError(null);
      const res = await donationApi.getParticipatingDonors({
        search: search.trim() ? search.trim() : undefined,
        donorType: type === 'ALL' ? undefined : type
      });

      if (res.success && res.data) {
        setDonors(res.data);
      } else {
        setDonors([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch participating donors.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDonors(searchQuery, selectedType);
  }, [selectedType]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    fetchDonors(searchQuery, selectedType);
  };

  const handleResetFilters = () => {
    setSearchQuery('');
    setSelectedType('ALL');
    fetchDonors('', 'ALL');
  };

  // Scenario 4: Select a Donor
  const handleSelectDonor = async (donor) => {
    setSelectedDonor(donor);
    setDetailLoading(true);
    setDetailError(null);
    setDetailData(null);

    try {
      const res = await donationApi.getDonorProfile(donor.donorId);
      if (res.success && res.data) {
        setDetailData(res.data);
      } else {
        setDetailData(donor);
      }
    } catch (err) {
      // Fallback to donor summary card data
      setDetailData(donor);
    } finally {
      setDetailLoading(false);
    }
  };

  return (
    <div className="donor-discovery-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner org-theme-banner">
        <div className="container">
          <div className="badge badge-amber" style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
            <Users size={14} />
            <span>Community Food Partners</span>
          </div>
          <h1 className="dashboard-title">Participating Food Donors</h1>
          <p className="dashboard-subtitle">
            Discover verified hotels, bakeries, and restaurants in the RescuePlate network contributing fresh surplus food to support local communities.
          </p>

          {/* Quick tab switcher between Browse Food & Browse Donors */}
          <div style={{ display: 'flex', gap: '12px', marginTop: '20px' }}>
            <Link 
              to="/browse-food" 
              className="btn btn-outline"
              style={{ background: 'rgba(255,255,255,0.15)', color: '#fff', borderColor: 'rgba(255,255,255,0.4)', borderRadius: '24px', padding: '8px 18px', fontSize: '0.875rem' }}
            >
              Browse Surplus Food Listings
            </Link>
            <div 
              style={{ background: '#fff', color: '#111827', fontWeight: 700, borderRadius: '24px', padding: '8px 18px', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <Store size={15} color="#d97706" />
              <span>Browse Donors Directory</span>
            </div>
          </div>
        </div>
      </div>

      <div className="container dashboard-body" style={{ maxWidth: '1200px', margin: '32px auto 80px' }}>
        {/* Filter & Search Bar */}
        <div style={{ background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb', padding: '20px', marginBottom: '28px', boxShadow: '0 2px 8px rgba(0,0,0,0.02)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px', marginBottom: '16px' }}>
            <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '10px', flex: '1 1 320px', maxWidth: '480px' }}>
              <div style={{ position: 'relative', width: '100%' }}>
                <Search size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }} />
                <input
                  type="text"
                  placeholder="Search donor business name or location..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '8px 12px 8px 36px',
                    borderRadius: '8px',
                    border: '1px solid #d1d5db',
                    fontSize: '0.875rem'
                  }}
                />
              </div>
              <button type="submit" className="btn btn-amber btn-sm" style={{ padding: '0 16px' }}>
                Search
              </button>
            </form>

            <button 
              onClick={() => fetchDonors(searchQuery, selectedType)}
              className="btn btn-outline btn-sm"
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
              <span>Refresh</span>
            </button>
          </div>

          {/* Type Filter Chips */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', alignItems: 'center', borderTop: '1px solid #f3f4f6', paddingTop: '16px' }}>
            <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#4b5563', marginRight: '4px' }}>
              Donor Type:
            </span>
            {donorTypes.map(type => (
              <button
                key={type}
                onClick={() => setSelectedType(type)}
                style={{
                  padding: '6px 14px',
                  borderRadius: '20px',
                  border: '1px solid',
                  borderColor: selectedType === type ? '#d97706' : '#e5e7eb',
                  background: selectedType === type ? '#fffbeb' : '#fff',
                  color: selectedType === type ? '#b45309' : '#4b5563',
                  fontSize: '0.8rem',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                {type === 'ALL' ? 'All Donor Types' : type}
              </button>
            ))}
          </div>
        </div>

        {/* Donors Listing Body */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: '60px 0' }}>
            <div className="loading-spinner" style={{ margin: '0 auto 16px' }} />
            <p style={{ color: '#6b7280' }}>Loading participating food donors...</p>
          </div>
        ) : error ? (
          <div style={{ padding: '24px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '12px', color: '#991b1b', textAlign: 'center' }}>
            <AlertCircle size={28} style={{ margin: '0 auto 8px', display: 'block' }} />
            <p style={{ margin: 0, fontWeight: 600 }}>{error}</p>
          </div>
        ) : donors.length === 0 ? (
          /* Scenario 3: No Donors Available */
          <div style={{ textAlign: 'center', padding: '60px 20px', background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb' }}>
            <Store size={48} style={{ color: '#d1d5db', margin: '0 auto 16px' }} />
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', marginBottom: '8px' }}>
              No Donors Found Matching Your Search
            </h3>
            <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
              We couldn't find any participating food donors matching "{searchQuery || selectedType}". Try adjusting your keywords or clearing filters.
            </p>
            <button onClick={handleResetFilters} className="btn btn-outline btn-sm">
              Clear Filters
            </button>
          </div>
        ) : (
          /* Scenario 1 & 2: View Donor Listing & Display Permitted Information */
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '24px' }}>
            {donors.map(donor => (
              <div 
                key={donor.donorId}
                style={{
                  background: '#fff',
                  borderRadius: '14px',
                  border: '1px solid #e5e7eb',
                  boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                  padding: '24px',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between',
                  transition: 'transform 0.15s ease, box-shadow 0.15s ease'
                }}
              >
                <div>
                  {/* Top Avatar & Type Header */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                      {donor.profilePictureUrl ? (
                        <img 
                          src={getProfileImageUrl(donor.profilePictureUrl)} 
                          alt={donor.businessName}
                          style={{ width: '48px', height: '48px', borderRadius: '50%', objectFit: 'cover', border: '2px solid #f3f4f6' }} 
                        />
                      ) : (
                        <div style={{ 
                          width: '48px', 
                          height: '48px', 
                          borderRadius: '50%', 
                          background: '#fef3c7', 
                          color: '#b45309', 
                          display: 'flex', 
                          alignItems: 'center', 
                          justifyContent: 'center',
                          fontWeight: 800,
                          fontSize: '1.2rem',
                          border: '2px solid #fde68a'
                        }}>
                          {donor.businessName ? donor.businessName.charAt(0).toUpperCase() : 'D'}
                        </div>
                      )}
                      <div>
                        <h3 style={{ margin: 0, fontSize: '1.15rem', fontWeight: 800, color: '#111827', display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <span>{donor.businessName}</span>
                          <ShieldCheck size={16} color="#059669" title="Verified RescuePlate Donor" />
                        </h3>
                        <span style={{ fontSize: '0.78rem', color: '#6b7280' }}>
                          Verified Food Contributor
                        </span>
                      </div>
                    </div>

                    <span 
                      style={{
                        fontSize: '0.75rem',
                        fontWeight: 700,
                        padding: '4px 10px',
                        borderRadius: '12px',
                        background: '#f3f4f6',
                        color: '#374151',
                        border: '1px solid #e5e7eb'
                      }}
                    >
                      {donor.donorType || 'Food Business'}
                    </span>
                  </div>

                  {/* Location & Bio */}
                  {donor.location && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#4b5563', fontSize: '0.85rem', marginBottom: '10px' }}>
                      <MapPin size={15} color="#9ca3af" style={{ flexShrink: 0 }} />
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {donor.location}
                      </span>
                    </div>
                  )}

                  <p style={{ 
                    color: '#6b7280', 
                    fontSize: '0.875rem', 
                    lineHeight: '1.45', 
                    margin: '0 0 16px',
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden'
                  }}>
                    {donor.bio || 'Participating entity dedicated to rescuing surplus food and redistributing to verified charities.'}
                  </p>

                  {/* Highlights Bar */}
                  <div style={{ background: '#f9fafb', borderRadius: '10px', padding: '12px', display: 'flex', justifyContent: 'space-around', textAlign: 'center', marginBottom: '18px' }}>
                    <div>
                      <div style={{ fontSize: '1.1rem', fontWeight: 800, color: '#047857' }}>
                        {donor.activeDonationsCount}
                      </div>
                      <div style={{ fontSize: '0.72rem', color: '#6b7280', fontWeight: 600 }}>Active Listings</div>
                    </div>
                    <div style={{ width: '1px', background: '#e5e7eb' }}></div>
                    <div>
                      <div style={{ fontSize: '1.1rem', fontWeight: 800, color: '#b45309' }}>
                        {donor.totalPortionsContributed}
                      </div>
                      <div style={{ fontSize: '0.72rem', color: '#6b7280', fontWeight: 600 }}>Portions Donated</div>
                    </div>
                  </div>
                </div>

                {/* Scenario 4: Select Donor Action Button */}
                <button
                  onClick={() => handleSelectDonor(donor)}
                  className="btn btn-outline full-width"
                  style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontSize: '0.875rem', fontWeight: 600 }}
                >
                  <span>View Profile & Food</span>
                  <ChevronRight size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Scenario 4: Donor Details Modal */}
      {selectedDonor && (
        <div 
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0,0,0,0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '20px'
          }}
        >
          <div 
            style={{
              background: '#fff',
              borderRadius: '16px',
              maxWidth: '560px',
              width: '100%',
              padding: '28px',
              boxShadow: '0 20px 25px -5px rgba(0,0,0,0.2)',
              maxHeight: '90vh',
              overflowY: 'auto'
            }}
          >
            {/* Modal Header */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <div style={{ 
                  width: '52px', 
                  height: '52px', 
                  borderRadius: '50%', 
                  background: '#fef3c7', 
                  color: '#b45309', 
                  display: 'flex', 
                  alignItems: 'center', 
                  justifyContent: 'center',
                  fontWeight: 800,
                  fontSize: '1.3rem',
                  border: '2px solid #fde68a'
                }}>
                  {selectedDonor.businessName ? selectedDonor.businessName.charAt(0).toUpperCase() : 'D'}
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: '1.3rem', fontWeight: 800, color: '#111827', display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <span>{selectedDonor.businessName}</span>
                    <ShieldCheck size={18} color="#059669" />
                  </h3>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '2px' }}>
                    <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#b45309', background: '#fffbeb', padding: '2px 8px', borderRadius: '8px' }}>
                      {selectedDonor.donorType || 'Food Business'}
                    </span>
                    {selectedDonor.memberSince && (
                      <span style={{ fontSize: '0.78rem', color: '#6b7280' }}>
                        Partner since {new Date(selectedDonor.memberSince).getFullYear()}
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <button 
                onClick={() => setSelectedDonor(null)}
                style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer', padding: '4px' }}
              >
                <X size={20} />
              </button>
            </div>

            {/* Modal Body */}
            {detailLoading ? (
              <div style={{ textAlign: 'center', padding: '40px 0' }}>
                <div className="loading-spinner" style={{ margin: '0 auto 12px' }} />
                <p style={{ color: '#6b7280', fontSize: '0.875rem' }}>Fetching full donor profile details...</p>
              </div>
            ) : (
              <div>
                {/* Location */}
                {selectedDonor.location && (
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#374151', fontSize: '0.9rem', marginBottom: '14px', background: '#f9fafb', padding: '10px 14px', borderRadius: '8px' }}>
                    <MapPin size={16} color="#d97706" style={{ flexShrink: 0 }} />
                    <strong>{selectedDonor.location}</strong>
                  </div>
                )}

                {/* Bio / Description */}
                <div style={{ marginBottom: '20px' }}>
                  <label style={{ fontSize: '0.8rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                    About This Donor
                  </label>
                  <p style={{ color: '#4b5563', fontSize: '0.925rem', lineHeight: '1.5', margin: '6px 0 0' }}>
                    {selectedDonor.bio || 'Verified food donor actively providing fresh portions to charitable partners through RescuePlate.'}
                  </p>
                </div>

                {/* Contributor Impact Stats */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '24px' }}>
                  <div style={{ background: '#ecfdf5', borderRadius: '10px', padding: '14px', border: '1px solid #a7f3d0' }}>
                    <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#065f46' }}>
                      {detailData?.activeDonationsCount ?? selectedDonor.activeDonationsCount}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: '#047857', fontWeight: 600 }}>Active Surplus Food Listings</div>
                  </div>
                  <div style={{ background: '#fffbeb', borderRadius: '10px', padding: '14px', border: '1px solid #fde68a' }}>
                    <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#92400e' }}>
                      {detailData?.totalPortionsContributed ?? selectedDonor.totalPortionsContributed}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: '#b45309', fontWeight: 600 }}>Total Portions Donated</div>
                  </div>
                </div>

                {/* Active Listings by This Donor */}
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                    <label style={{ fontSize: '0.8rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                      Active Food Items Available Right Now
                    </label>
                    <Link to="/browse-food" style={{ fontSize: '0.8rem', color: '#d97706', fontWeight: 600, textDecoration: 'none' }}>
                      View all food &rarr;
                    </Link>
                  </div>

                  {detailData?.activeListings && detailData.activeListings.length > 0 ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                      {detailData.activeListings.map(item => (
                        <div 
                          key={item.id}
                          style={{
                            padding: '10px 14px',
                            background: '#f9fafb',
                            borderRadius: '8px',
                            border: '1px solid #e5e7eb',
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center'
                          }}
                        >
                          <div>
                            <div style={{ fontWeight: 700, color: '#111827', fontSize: '0.925rem' }}>
                              {item.foodTitle}
                            </div>
                            <div style={{ fontSize: '0.78rem', color: '#6b7280' }}>
                              {item.category} &bull; {item.remainingQuantity} {item.unit} available
                            </div>
                          </div>
                          <Link 
                            to="/browse-food" 
                            className="btn btn-amber btn-sm"
                            style={{ fontSize: '0.78rem', padding: '4px 10px' }}
                          >
                            Claim Food
                          </Link>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div style={{ padding: '16px', background: '#f9fafb', borderRadius: '8px', textAlign: 'center', color: '#6b7280', fontSize: '0.85rem' }}>
                      No active listings right now. Check back soon or visit <Link to="/browse-food" style={{ color: '#d97706' }}>Browse Food</Link>.
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Modal Footer */}
            <div style={{ borderTop: '1px solid #f3f4f6', paddingTop: '18px', marginTop: '24px', display: 'flex', justifyContent: 'flex-end' }}>
              <button 
                onClick={() => setSelectedDonor(null)}
                className="btn btn-outline btn-md"
              >
                Close Profile
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
