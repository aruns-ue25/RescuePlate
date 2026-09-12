import React, { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
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
  Send,
  Award,
  HelpingHand,
  Eye,
  Mail,
  Phone
} from 'lucide-react';

export default function OrganizationDiscoveryPage() {
  const { currentUser } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  const [organizations, setOrganizations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Search & Filter States
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('ALL');

  // Selected Organization Modal State (Scenario 4)
  const [selectedOrg, setSelectedOrg] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailData, setDetailData] = useState(null);

  const foodCategories = [
    'ALL', 
    'Cooked Meals', 
    'Bakery', 
    'Fresh Produce', 
    'Dairy & Chilled', 
    'Packaged Dry'
  ];

  const fetchOrganizations = async (search = searchQuery, category = selectedCategory) => {
    try {
      setLoading(true);
      setError(null);
      const res = await donationApi.getParticipatingOrganizations({
        search: search.trim() ? search.trim() : undefined,
        foodCategory: category === 'ALL' ? undefined : category
      });

      if (res.success && res.data) {
        setOrganizations(res.data);
      } else {
        setOrganizations([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch participating charitable organizations.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOrganizations(searchQuery, selectedCategory);
  }, [selectedCategory]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    fetchOrganizations(searchQuery, selectedCategory);
  };

  const handleResetFilters = () => {
    setSearchQuery('');
    setSelectedCategory('ALL');
    fetchOrganizations('', 'ALL');
  };

  // Scenario 2 & 4: Select Organization
  const handleSelectOrg = async (org) => {
    setSelectedOrg(org);
    setDetailLoading(true);
    setDetailData(null);

    try {
      const res = await donationApi.getOrganizationProfile(org.organizationId);
      if (res.success && res.data) {
        setDetailData(res.data);
      } else {
        setDetailData(org);
      }
    } catch (err) {
      setDetailData(org);
    } finally {
      setDetailLoading(false);
    }
  };

  const handleSelectOrgById = async (id) => {
    setDetailLoading(true);
    setDetailData(null);
    try {
      const res = await donationApi.getOrganizationProfile(id);
      if (res.success && res.data) {
        setSelectedOrg(res.data);
        setDetailData(res.data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setDetailLoading(false);
    }
  };

  useEffect(() => {
    const orgIdParam = searchParams.get('orgId') || searchParams.get('view');
    if (orgIdParam && (!selectedOrg || selectedOrg.organizationId !== orgIdParam)) {
      const found = organizations.find(o => o.organizationId === orgIdParam);
      if (found) {
        handleSelectOrg(found);
      } else {
        handleSelectOrgById(orgIdParam);
      }
    }
  }, [searchParams, organizations]);

  const handleCloseModal = () => {
    setSelectedOrg(null);
    setDetailData(null);
    if (searchParams.get('orgId') || searchParams.get('view')) {
      const nextParams = new URLSearchParams(searchParams);
      nextParams.delete('orgId');
      nextParams.delete('view');
      setSearchParams(nextParams);
    }
  };

  return (
    <div className="organization-discovery-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner donor-theme-banner" style={{ background: 'linear-gradient(135deg, #064e3b 0%, #047857 100%)', color: '#fff', padding: '40px 0' }}>
        <div className="container">
          <div className="badge badge-primary" style={{ background: 'rgba(255,255,255,0.2)', color: '#fff', border: 'none', marginBottom: '10px', display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
            <HelpingHand size={14} />
            <span>Charity & Community Partner Discovery</span>
          </div>
          <h1 className="dashboard-title" style={{ color: '#fff', fontSize: '2rem', fontWeight: 800, margin: '4px 0 8px' }}>
            Participating Charitable Organizations
          </h1>
          <p className="dashboard-subtitle" style={{ color: '#d1fae5', margin: 0, fontSize: '1rem', maxWidth: '700px' }}>
            Discover verified food banks, shelters, and community kitchens ready to receive, transport, and redistribute your surplus food to vulnerable families.
          </p>

          {/* Quick tab switchers */}
          <div style={{ display: 'flex', gap: '12px', marginTop: '20px', flexWrap: 'wrap' }}>
            <Link 
              to="/donor-portal" 
              className="btn btn-outline"
              style={{ background: 'rgba(255,255,255,0.15)', color: '#fff', borderColor: 'rgba(255,255,255,0.4)', borderRadius: '24px', padding: '8px 18px', fontSize: '0.875rem' }}
            >
              My Surplus Food Listings
            </Link>
            <div 
              style={{ background: '#fff', color: '#064e3b', fontWeight: 700, borderRadius: '24px', padding: '8px 18px', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <Users size={15} color="#047857" />
              <span>Browse Charities Directory</span>
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
                  placeholder="Search charity name, location, or mission..."
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
              <button type="submit" className="btn btn-primary btn-sm" style={{ padding: '0 16px', background: '#047857' }}>
                Search
              </button>
            </form>

            <button 
              onClick={() => fetchOrganizations(searchQuery, selectedCategory)}
              className="btn btn-outline btn-sm"
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
              <span>Refresh</span>
            </button>
          </div>

          {/* Food Category Filters */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', alignItems: 'center', borderTop: '1px solid #f3f4f6', paddingTop: '16px' }}>
            <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#4b5563', marginRight: '4px' }}>
              Accepted Food Types:
            </span>
            {foodCategories.map(cat => (
              <button
                key={cat}
                onClick={() => setSelectedCategory(cat)}
                style={{
                  padding: '6px 14px',
                  borderRadius: '20px',
                  border: '1px solid',
                  borderColor: selectedCategory === cat ? '#047857' : '#e5e7eb',
                  background: selectedCategory === cat ? '#ecfdf5' : '#fff',
                  color: selectedCategory === cat ? '#065f46' : '#4b5563',
                  fontSize: '0.8rem',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                {cat === 'ALL' ? 'All Food Types' : cat}
              </button>
            ))}
          </div>
        </div>

        {/* Organizations Listing Body */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: '60px 0' }}>
            <div className="loading-spinner" style={{ margin: '0 auto 16px' }} />
            <p style={{ color: '#6b7280' }}>Loading participating organizations...</p>
          </div>
        ) : error ? (
          <div style={{ padding: '24px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '12px', color: '#991b1b', textAlign: 'center' }}>
            <AlertCircle size={28} style={{ margin: '0 auto 8px', display: 'block' }} />
            <p style={{ margin: 0, fontWeight: 600 }}>{error}</p>
          </div>
        ) : organizations.length === 0 ? (
          /* Scenario 3: No Organizations Available */
          <div style={{ textAlign: 'center', padding: '60px 20px', background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb' }}>
            <Users size={48} style={{ color: '#d1d5db', margin: '0 auto 16px' }} />
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', marginBottom: '8px' }}>
              No Organizations Found Matching Your Criteria
            </h3>
            <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
              We couldn't find any participating charities accepting "{selectedCategory !== 'ALL' ? selectedCategory : searchQuery}". Try adjusting keywords or clearing category filters.
            </p>
            <button onClick={handleResetFilters} className="btn btn-outline btn-sm">
              Clear Filters
            </button>
          </div>
        ) : (
          /* Scenario 1 & 2: View Organization Listing & Display Information */
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))', gap: '24px' }}>
            {organizations.map(org => (
              <div 
                key={org.organizationId}
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
                  {/* Top Header */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '14px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                      <div style={{ 
                        width: '48px', 
                        height: '48px', 
                        borderRadius: '50%', 
                        background: '#ecfdf5', 
                        color: '#065f46', 
                        display: 'flex', 
                        alignItems: 'center', 
                        justifyContent: 'center',
                        fontWeight: 800,
                        fontSize: '1.2rem',
                        border: '2px solid #a7f3d0'
                      }}>
                        {org.organizationName ? org.organizationName.charAt(0).toUpperCase() : 'O'}
                      </div>
                      <div>
                        <h3 style={{ margin: 0, fontSize: '1.15rem', fontWeight: 800, color: '#111827', display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <span>{org.organizationName}</span>
                          <ShieldCheck size={16} color="#059669" title="Verified Charitable Entity" />
                        </h3>
                        <span style={{ fontSize: '0.78rem', color: '#6b7280' }}>
                          Registered Non-Profit Partner
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
                      {org.organizationType}
                    </span>
                  </div>

                  {/* Location Address */}
                  {org.location && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#4b5563', fontSize: '0.85rem', marginBottom: '10px' }}>
                      <MapPin size={15} color="#9ca3af" style={{ flexShrink: 0 }} />
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {org.location}
                      </span>
                    </div>
                  )}

                  {/* Description / Mission */}
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
                    {org.description || 'Community organization focused on food recovery and serving meals to those facing hunger.'}
                  </p>

                  {/* Accepted Food Types Tags */}
                  <div style={{ marginBottom: '16px' }}>
                    <div style={{ fontSize: '0.75rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase', marginBottom: '6px' }}>
                      Accepts:
                    </div>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                      {org.acceptedFoodTypes?.map(tag => (
                        <span 
                          key={tag}
                          style={{
                            fontSize: '0.72rem',
                            fontWeight: 600,
                            padding: '2px 8px',
                            borderRadius: '8px',
                            background: '#f0fdf4',
                            color: '#166534',
                            border: '1px solid #bbf7d0'
                          }}
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  </div>

                  {/* Impact Bar */}
                  <div style={{ background: '#f9fafb', borderRadius: '10px', padding: '12px', display: 'flex', justifyContent: 'space-around', textAlign: 'center', marginBottom: '18px' }}>
                    <div>
                      <div style={{ fontSize: '1.1rem', fontWeight: 800, color: '#047857' }}>
                        {org.claimedDonationsCount}
                      </div>
                      <div style={{ fontSize: '0.72rem', color: '#6b7280', fontWeight: 600 }}>Food Rescues</div>
                    </div>
                    <div style={{ width: '1px', background: '#e5e7eb' }}></div>
                    <div>
                      <div style={{ fontSize: '1.1rem', fontWeight: 800, color: '#0369a1' }}>
                        {org.totalPortionsReceived}
                      </div>
                      <div style={{ fontSize: '0.72rem', color: '#6b7280', fontWeight: 600 }}>Portions Distributed</div>
                    </div>
                  </div>
                </div>

                {/* Scenario 4: Select Organization Button */}
                <button
                  onClick={() => handleSelectOrg(org)}
                  className="btn btn-outline full-width"
                  style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontSize: '0.875rem', fontWeight: 600 }}
                >
                  <span>View Organization Profile</span>
                  <ChevronRight size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Scenario 4: Selected Organization Detail Modal */}
      {selectedOrg && (
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
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                {selectedOrg.profilePictureUrl ? (
                  <img 
                    src={getProfileImageUrl(selectedOrg.profilePictureUrl)} 
                    alt={selectedOrg.organizationName}
                    style={{ width: '52px', height: '52px', borderRadius: '50%', objectFit: 'cover', border: '2px solid #a7f3d0' }} 
                  />
                ) : (
                  <div style={{ 
                    width: '52px', 
                    height: '52px', 
                    borderRadius: '50%', 
                    background: '#ecfdf5', 
                    color: '#065f46', 
                    display: 'flex', 
                    alignItems: 'center', 
                    justifyContent: 'center',
                    fontWeight: 800,
                    fontSize: '1.3rem',
                    border: '2px solid #a7f3d0'
                  }}>
                    {selectedOrg.organizationName ? selectedOrg.organizationName.charAt(0).toUpperCase() : 'O'}
                  </div>
                )}
                <div>
                  <h3 style={{ margin: 0, fontSize: '1.3rem', fontWeight: 800, color: '#111827', display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <span>{detailData?.organizationName || selectedOrg.organizationName}</span>
                    <ShieldCheck size={18} color="#059669" />
                  </h3>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '2px' }}>
                    <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#065f46', background: '#ecfdf5', padding: '2px 8px', borderRadius: '8px' }}>
                      {detailData?.organizationType || selectedOrg.organizationType}
                    </span>
                    {(detailData?.memberSince || selectedOrg.memberSince) && (
                      <span style={{ fontSize: '0.78rem', color: '#6b7280' }}>
                        Partner since {new Date(detailData?.memberSince || selectedOrg.memberSince).getFullYear()}
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <button 
                onClick={handleCloseModal}
                style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer', padding: '4px' }}
                title="Close profile"
              >
                <X size={20} />
              </button>
            </div>

            {/* Read-Only Status Indicator (Scenario 5) */}
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', background: '#f3f4f6', padding: '4px 12px', borderRadius: '16px', fontSize: '0.75rem', fontWeight: 600, color: '#4b5563', marginBottom: '16px', border: '1px solid #e5e7eb' }}>
              <Eye size={13} color="#6b7280" />
              <span>Verified Community Partner &bull; Read-Only Directory View</span>
            </div>

            {/* Modal Body */}
            {detailLoading ? (
              <div style={{ textAlign: 'center', padding: '40px 0' }}>
                <div className="loading-spinner" style={{ margin: '0 auto 12px' }} />
                <p style={{ color: '#6b7280', fontSize: '0.875rem' }}>Fetching full organization details...</p>
              </div>
            ) : (
              <div>
                {/* Location */}
                {(detailData?.location || selectedOrg.location) && (
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#374151', fontSize: '0.9rem', marginBottom: '14px', background: '#f9fafb', padding: '10px 14px', borderRadius: '8px' }}>
                    <MapPin size={16} color="#047857" style={{ flexShrink: 0 }} />
                    <strong>{detailData?.location || selectedOrg.location}</strong>
                  </div>
                )}

                {/* Contact Information (Scenario 3) */}
                {(detailData?.contactEmail || selectedOrg.contactEmail || detailData?.contactPhone || selectedOrg.contactPhone) && (
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '16px', fontSize: '0.85rem', color: '#4b5563', marginBottom: '16px', background: '#f9fafb', padding: '10px 14px', borderRadius: '8px', border: '1px solid #e5e7eb' }}>
                    {(detailData?.contactEmail || selectedOrg.contactEmail) && (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <Mail size={14} color="#6b7280" />
                        <span>{detailData?.contactEmail || selectedOrg.contactEmail}</span>
                      </div>
                    )}
                    {(detailData?.contactPhone || selectedOrg.contactPhone) && (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <Phone size={14} color="#6b7280" />
                        <span>{detailData?.contactPhone || selectedOrg.contactPhone}</span>
                      </div>
                    )}
                  </div>
                )}

                {/* Mission / Description */}
                <div style={{ marginBottom: '20px' }}>
                  <label style={{ fontSize: '0.8rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                    About This Organization & Mission
                  </label>
                  <p style={{ color: '#4b5563', fontSize: '0.925rem', lineHeight: '1.5', margin: '6px 0 0' }}>
                    {detailData?.description || selectedOrg.description || 'Dedicated non-profit entity providing emergency meals and community hunger relief.'}
                  </p>
                </div>

                {/* Accepted Food Types (Scenario 4) */}
                <div style={{ marginBottom: '20px' }}>
                  <label style={{ fontSize: '0.8rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em', display: 'block', marginBottom: '8px' }}>
                    Food Categories Accepted For Donation
                  </label>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                    {(detailData?.acceptedFoodTypes || selectedOrg.acceptedFoodTypes)?.map(cat => (
                      <span 
                        key={cat}
                        style={{
                          fontSize: '0.8rem',
                          fontWeight: 600,
                          padding: '4px 10px',
                          borderRadius: '8px',
                          background: '#ecfdf5',
                          color: '#065f46',
                          border: '1px solid #a7f3d0'
                        }}
                      >
                        ✓ {cat}
                      </span>
                    ))}
                  </div>
                </div>

                {/* Impact Metrics */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '24px' }}>
                  <div style={{ background: '#f0fdf4', borderRadius: '10px', padding: '14px', border: '1px solid #bbf7d0' }}>
                    <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#166534' }}>
                      {detailData?.claimedDonationsCount ?? selectedOrg.claimedDonationsCount}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: '#15803d', fontWeight: 600 }}>Food Rescues Completed</div>
                  </div>
                  <div style={{ background: '#f0f9ff', borderRadius: '10px', padding: '14px', border: '1px solid #bae6fd' }}>
                    <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#0369a1' }}>
                      {detailData?.totalPortionsReceived ?? selectedOrg.totalPortionsReceived}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: '#0284c7', fontWeight: 600 }}>Total Portions Distributed</div>
                  </div>
                </div>

                {/* Post Food Shortcut */}
                <div style={{ background: '#f9fafb', borderRadius: '10px', padding: '16px', border: '1px solid #e5e7eb', textAlign: 'center' }}>
                  <p style={{ margin: '0 0 10px', color: '#4b5563', fontSize: '0.875rem' }}>
                    Have surplus food matching this organization's accepted categories?
                  </p>
                  <Link 
                    to="/donor-portal" 
                    className="btn btn-primary btn-sm"
                    style={{ background: '#047857', border: 'none', display: 'inline-flex', alignItems: 'center', gap: '6px' }}
                  >
                    <Package size={15} />
                    <span>Post Surplus Food Now</span>
                  </Link>
                </div>
              </div>
            )}

            {/* Modal Footer */}
            <div style={{ borderTop: '1px solid #f3f4f6', paddingTop: '18px', marginTop: '24px', display: 'flex', justifyContent: 'flex-end' }}>
              <button 
                onClick={handleCloseModal}
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
