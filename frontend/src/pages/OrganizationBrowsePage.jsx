import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { donationApi } from '../services/api';
import { 
  HeartHandshake, 
  Sparkles, 
  PackageSearch, 
  ArrowRight,
  Clock,
  MapPin,
  Building2,
  Calendar,
  AlertCircle,
  CheckCircle2,
  RefreshCw,
  Search,
  Filter,
  Send,
  X,
  ShieldCheck,
  Tag,
  Users,
  Eye,
  Info,
  Check
} from 'lucide-react';

export default function OrganizationBrowsePage() {
  const { currentUser } = useAuth();

  const [donations, setDonations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filter States
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('ALL');

  // Selected Donation Details Modal State (Scenario 5)
  const [selectedDonation, setSelectedDonation] = useState(null);

  // Request/Claim Modal State
  const [claimDonation, setClaimDonation] = useState(null);
  const [claimQuantity, setClaimQuantity] = useState(1);
  const [claimSubmitting, setClaimSubmitting] = useState(false);
  const [claimError, setClaimError] = useState(null);
  const [claimSuccess, setClaimSuccess] = useState(null);

  const fetchAvailableDonations = async (category = categoryFilter, search = searchQuery) => {
    try {
      setLoading(true);
      setError(null);
      const res = await donationApi.getAvailableDonations(
        category === 'ALL' ? undefined : category,
        search.trim() ? search.trim() : undefined
      );
      if (res.success && res.data) {
        setDonations(res.data);
      } else {
        setDonations([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch available surplus food listings.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAvailableDonations(categoryFilter, searchQuery);
  }, [categoryFilter]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    fetchAvailableDonations(categoryFilter, searchQuery);
  };

  const handleOpenClaimModal = (item) => {
    // Check client-side expiry check
    const isExpired = new Date(item.expiryTime) <= new Date();
    if (isExpired || item.status === 'Expired') {
      alert('This food donation has reached the end of its availability period and cannot be requested.');
      return;
    }

    setClaimDonation(item);
    setClaimQuantity(Math.min(item.remainingQuantity, 1));
    setClaimError(null);
    setClaimSuccess(null);
  };

  const handleConfirmClaim = async (e) => {
    e.preventDefault();
    if (!claimDonation) return;

    // Validate client-side before sending
    if (new Date(claimDonation.expiryTime) <= new Date()) {
      setClaimError('This donation has just expired and can no longer accept requests.');
      return;
    }

    if (claimQuantity <= 0) {
      setClaimError('Requested portion quantity must be greater than 0.');
      return;
    }

    if (claimQuantity > claimDonation.remainingQuantity) {
      setClaimError(`Requested quantity (${claimQuantity}) exceeds remaining available portions (${claimDonation.remainingQuantity} ${claimDonation.unit}).`);
      return;
    }

    try {
      setClaimSubmitting(true);
      setClaimError(null);
      const res = await donationApi.requestDonation(claimDonation.id, { quantity: Number(claimQuantity) });
      if (res.success) {
        setClaimSuccess(`Successfully requested ${claimQuantity} ${claimDonation.unit}! Notification sent to donor.`);
        fetchAvailableDonations(categoryFilter, searchQuery);
        setTimeout(() => {
          setClaimDonation(null);
          setClaimSuccess(null);
        }, 1500);
      } else {
        setClaimError(res.message || 'Failed to submit food request.');
      }
    } catch (err) {
      setClaimError(err.message || err.errors?.[0] || 'An error occurred while requesting the food donation.');
    } finally {
      setClaimSubmitting(false);
    }
  };

  const formatExpiryTime = (isoString) => {
    if (!isoString) return 'N/A';
    const date = new Date(isoString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const calculateHoursLeft = (isoString) => {
    if (!isoString) return { expired: true, text: 'Expired' };
    const diff = new Date(isoString) - new Date();
    if (diff <= 0) return { expired: true, text: 'Expired' };
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    return { expired: false, text: `${hours}h ${mins}m left` };
  };

  return (
    <div className="browse-food-page animate-fade-in-up">
      {/* Header Banner */}
      <div className="dashboard-header-banner org-theme-banner">
        <div className="container">
          <div className="badge badge-amber">
            <HeartHandshake size={14} />
            <span>Charity Surplus Food Marketplace</span>
          </div>
          <h1 className="dashboard-title">Browse Available Surplus Food</h1>
          <p className="dashboard-subtitle">
            Connect directly with verified local restaurants, hotels, and bakeries to claim fresh surplus food within active availability periods.
          </p>

          <div style={{ display: 'flex', gap: '12px', marginTop: '20px' }}>
            <div 
              style={{ background: '#fff', color: '#111827', fontWeight: 700, borderRadius: '24px', padding: '8px 18px', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <HeartHandshake size={15} color="#d97706" />
              <span>Browse Surplus Food</span>
            </div>
            <Link 
              to="/donors" 
              className="btn btn-outline"
              style={{ background: 'rgba(255,255,255,0.15)', color: '#fff', borderColor: 'rgba(255,255,255,0.4)', borderRadius: '24px', padding: '8px 18px', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <Users size={14} />
              <span>Browse Donors Directory</span>
            </Link>
          </div>
        </div>
      </div>

      <div className="container dashboard-body" style={{ maxWidth: '1200px', margin: '32px auto 80px' }}>
        
        {/* Search & Category Filter Bar */}
        <div style={{ background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb', padding: '20px', marginBottom: '28px', boxShadow: '0 2px 8px rgba(0,0,0,0.02)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px', marginBottom: '16px' }}>
            <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '10px', flex: '1 1 320px', maxWidth: '480px' }}>
              <div style={{ position: 'relative', width: '100%' }}>
                <Search size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }} />
                <input
                  type="text"
                  placeholder="Search available food or donor..."
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
              onClick={() => fetchAvailableDonations(categoryFilter, searchQuery)}
              className="btn btn-outline btn-sm"
              style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            >
              <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
              <span>Refresh</span>
            </button>
          </div>

          {/* Categories */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', alignItems: 'center', borderTop: '1px solid #f3f4f6', paddingTop: '16px' }}>
            <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#4b5563', marginRight: '4px' }}>
              Categories:
            </span>
            {['ALL', 'Cooked Meals', 'Bakery', 'Produce', 'Dairy & Eggs', 'Packaged Food', 'Beverages'].map(cat => (
              <button
                key={cat}
                onClick={() => setCategoryFilter(cat)}
                style={{
                  padding: '6px 14px',
                  borderRadius: '20px',
                  border: '1px solid',
                  borderColor: categoryFilter === cat ? '#d97706' : '#e5e7eb',
                  background: categoryFilter === cat ? '#fffbeb' : '#fff',
                  color: categoryFilter === cat ? '#b45309' : '#4b5563',
                  fontSize: '0.8rem',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                {cat === 'ALL' ? 'All Surplus Food' : cat}
              </button>
            ))}
          </div>
        </div>

        {/* Listings Content */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: '60px 0' }}>
            <div className="loading-spinner" style={{ margin: '0 auto 16px' }} />
            <p style={{ color: '#6b7280' }}>Fetching active surplus food donations...</p>
          </div>
        ) : error ? (
          <div style={{ padding: '24px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '12px', color: '#991b1b', textAlign: 'center' }}>
            <AlertCircle size={28} style={{ margin: '0 auto 8px', display: 'block' }} />
            <p style={{ margin: 0, fontWeight: 600 }}>{error}</p>
          </div>
        ) : donations.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '60px 20px', background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb' }}>
            <PackageSearch size={48} style={{ color: '#d1d5db', margin: '0 auto 16px' }} />
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#1f2937', marginBottom: '8px' }}>
              No Available Surplus Food At This Moment
            </h3>
            <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
              {categoryFilter !== 'ALL' || searchQuery
                ? 'No food donations match your current search or category filter. Try clearing your filters or searching with different keywords.'
                : 'Donations may have already reached their availability period or have been fully claimed. Please check back shortly!'}
            </p>
            {(categoryFilter !== 'ALL' || searchQuery) && (
              <button
                onClick={() => {
                  setCategoryFilter('ALL');
                  setSearchQuery('');
                  fetchAvailableDonations('ALL', '');
                }}
                className="btn btn-outline btn-sm"
                style={{ margin: '0 auto', display: 'inline-flex', alignItems: 'center', gap: '6px' }}
              >
                <RefreshCw size={14} />
                <span>Clear Filters</span>
              </button>
            )}
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '24px' }}>
            {donations.map(item => {
              const timeLeft = calculateHoursLeft(item.expiryTime);
              return (
                <div 
                  key={item.id} 
                  style={{
                    background: '#fff',
                    borderRadius: '14px',
                    border: '1px solid #e5e7eb',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                    padding: '20px',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'space-between'
                  }}
                >
                  <div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
                      <span 
                        style={{
                          fontSize: '0.75rem',
                          fontWeight: 700,
                          padding: '3px 10px',
                          borderRadius: '12px',
                          background: '#ecfdf5',
                          color: '#065f46',
                          border: '1px solid #a7f3d0'
                        }}
                      >
                        {item.category}
                      </span>
                      <span 
                        style={{
                          fontSize: '0.75rem',
                          fontWeight: 700,
                          display: 'flex',
                          alignItems: 'center',
                          gap: '4px',
                          color: timeLeft.expired ? '#dc2626' : '#d97706'
                        }}
                      >
                        <Clock size={13} />
                        {timeLeft.text}
                      </span>
                    </div>

                    <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: '#111827', margin: '0 0 8px' }}>
                      {item.foodTitle}
                    </h3>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#047857', fontSize: '0.85rem', fontWeight: 600, marginBottom: '12px' }}>
                      <Building2 size={15} />
                      <Link 
                        to="/donors" 
                        style={{ color: '#047857', textDecoration: 'none', hover: { textDecoration: 'underline' } }}
                        title="View donor profile in directory"
                      >
                        {item.donorName || "Verified Food Donor"}
                      </Link>
                    </div>

                    <div style={{ background: '#f9fafb', borderRadius: '8px', padding: '12px', marginBottom: '16px' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', marginBottom: '6px' }}>
                        <span style={{ color: '#6b7280' }}>Available Portion:</span>
                        <strong style={{ color: '#047857', fontSize: '1rem' }}>
                          {item.remainingQuantity} {item.unit}
                        </strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.82rem' }}>
                        <span style={{ color: '#6b7280' }}>Availability Deadline:</span>
                        <span style={{ color: '#374151', fontWeight: 600 }}>{formatExpiryTime(item.expiryTime)}</span>
                      </div>
                    </div>

                    {item.location && (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#6b7280', fontSize: '0.8rem', marginBottom: '8px' }}>
                        <MapPin size={14} style={{ flexShrink: 0 }} />
                        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{item.location}</span>
                      </div>
                    )}
                    
                    {item.dietaryTags && (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#4b5563', fontSize: '0.8rem', marginBottom: '16px' }}>
                        <Tag size={13} style={{ flexShrink: 0 }} />
                        <span>{item.dietaryTags}</span>
                      </div>
                    )}
                  </div>

                  {/* Actions: View Details & Request Portion */}
                  <div style={{ borderTop: '1px solid #f3f4f6', paddingTop: '16px', marginTop: '12px', display: 'flex', gap: '10px' }}>
                    <button
                      type="button"
                      onClick={() => setSelectedDonation(item)}
                      className="btn btn-outline btn-md"
                      style={{ flex: '1', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', fontSize: '0.875rem' }}
                    >
                      <Eye size={15} />
                      <span>Details</span>
                    </button>
                    <button
                      onClick={() => handleOpenClaimModal(item)}
                      className="btn btn-amber btn-md"
                      style={{ flex: '2', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontSize: '0.875rem' }}
                    >
                      <Send size={15} />
                      <span>Request</span>
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Request / Claim Modal */}
      {claimDonation && (
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
              maxWidth: '480px',
              width: '100%',
              padding: '28px',
              boxShadow: '0 20px 25px -5px rgba(0,0,0,0.2)'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <div style={{ width: '36px', height: '36px', borderRadius: '50%', background: '#fef3c7', color: '#d97706', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Send size={18} />
                </div>
                <h3 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 800, color: '#111827' }}>
                  Request Food Donation
                </h3>
              </div>
              <button 
                onClick={() => setClaimDonation(null)}
                style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer', padding: '4px' }}
              >
                <X size={20} />
              </button>
            </div>

            {claimError && (
              <div style={{ padding: '12px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#991b1b', fontSize: '0.875rem', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <AlertCircle size={16} style={{ flexShrink: 0 }} />
                <span>{claimError}</span>
              </div>
            )}

            {claimSuccess && (
              <div style={{ padding: '12px', background: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: '8px', color: '#166534', fontSize: '0.875rem', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <CheckCircle2 size={16} style={{ flexShrink: 0 }} />
                <span>{claimSuccess}</span>
              </div>
            )}

            <form onSubmit={handleConfirmClaim}>
              <div style={{ background: '#f9fafb', borderRadius: '8px', padding: '14px', marginBottom: '18px' }}>
                <div style={{ fontWeight: 700, color: '#111827', fontSize: '1.05rem', marginBottom: '4px' }}>
                  {claimDonation.foodTitle}
                </div>
                <div style={{ fontSize: '0.85rem', color: '#6b7280' }}>
                  Offered by: <strong>{claimDonation.donorName}</strong>
                </div>
                <div style={{ fontSize: '0.85rem', color: '#047857', fontWeight: 600, marginTop: '6px' }}>
                  Available to Claim: {claimDonation.remainingQuantity} {claimDonation.unit}
                </div>
              </div>

              <div style={{ marginBottom: '20px' }}>
                <label style={{ display: 'block', fontSize: '0.875rem', fontWeight: 700, color: '#374151', marginBottom: '8px' }}>
                  Quantity to Request ({claimDonation.unit}) *
                </label>
                <input
                  type="number"
                  min="1"
                  max={claimDonation.remainingQuantity}
                  value={claimQuantity}
                  onChange={(e) => setClaimQuantity(Number(e.target.value))}
                  required
                  style={{
                    width: '100%',
                    padding: '10px 14px',
                    borderRadius: '8px',
                    border: '1px solid #d1d5db',
                    fontSize: '1rem',
                    fontWeight: 600
                  }}
                />
                <span style={{ fontSize: '0.78rem', color: '#6b7280', marginTop: '4px', display: 'block' }}>
                  Enter how many portions your organization intends to collect and redistribute.
                </span>
              </div>

              <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
                <button
                  type="button"
                  onClick={() => setClaimDonation(null)}
                  disabled={claimSubmitting}
                  className="btn btn-outline btn-md"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={claimSubmitting || claimSuccess}
                  className="btn btn-amber btn-md"
                  style={{ minWidth: '130px' }}
                >
                  {claimSubmitting ? 'Requesting...' : 'Confirm Request'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Donation Details Modal (Scenario 5) */}
      {selectedDonation && (
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
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
              <div>
                <span 
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    padding: '3px 10px',
                    borderRadius: '12px',
                    background: '#ecfdf5',
                    color: '#065f46',
                    border: '1px solid #a7f3d0',
                    display: 'inline-block',
                    marginBottom: '8px'
                  }}
                >
                  {selectedDonation.category}
                </span>
                <h2 style={{ margin: 0, fontSize: '1.4rem', fontWeight: 800, color: '#111827' }}>
                  {selectedDonation.foodTitle}
                </h2>
              </div>
              <button 
                onClick={() => setSelectedDonation(null)}
                style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer', padding: '4px' }}
              >
                <X size={20} />
              </button>
            </div>

            {/* Donor Info Bar */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px 16px', background: '#f9fafb', borderRadius: '10px', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Building2 size={18} color="#047857" />
                <div>
                  <div style={{ fontSize: '0.75rem', color: '#6b7280' }}>Offered by Donor</div>
                  <Link 
                    to="/donors" 
                    style={{ fontWeight: 700, color: '#047857', textDecoration: 'none' }}
                  >
                    {selectedDonation.donorName || "Verified Food Donor"}
                  </Link>
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <span 
                  style={{
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: '4px',
                    color: calculateHoursLeft(selectedDonation.expiryTime).expired ? '#dc2626' : '#d97706'
                  }}
                >
                  <Clock size={13} />
                  {calculateHoursLeft(selectedDonation.expiryTime).text}
                </span>
              </div>
            </div>

            {/* Description / Notes */}
            {(selectedDonation.notes || selectedDonation.description) && (
              <div style={{ marginBottom: '18px' }}>
                <h4 style={{ fontSize: '0.85rem', fontWeight: 700, color: '#374151', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '6px' }}>
                  Preparation Notes & Instructions
                </h4>
                <p style={{ margin: 0, color: '#4b5563', fontSize: '0.925rem', lineHeight: 1.5, background: '#fdfdfd', border: '1px solid #f3f4f6', padding: '12px', borderRadius: '8px' }}>
                  {selectedDonation.notes || selectedDonation.description}
                </p>
              </div>
            )}

            {/* Quantity Metrics */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '18px' }}>
              <div style={{ background: '#ecfdf5', padding: '12px', borderRadius: '10px', border: '1px solid #a7f3d0' }}>
                <div style={{ fontSize: '0.75rem', color: '#065f46', fontWeight: 600 }}>Remaining Quantity</div>
                <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#065f46', marginTop: '2px' }}>
                  {selectedDonation.remainingQuantity} <span style={{ fontSize: '0.85rem', fontWeight: 600 }}>{selectedDonation.unit}</span>
                </div>
              </div>
              <div style={{ background: '#f9fafb', padding: '12px', borderRadius: '10px', border: '1px solid #e5e7eb' }}>
                <div style={{ fontSize: '0.75rem', color: '#6b7280', fontWeight: 600 }}>Total Prepared</div>
                <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#374151', marginTop: '2px' }}>
                  {selectedDonation.totalQuantity ?? selectedDonation.quantity} <span style={{ fontSize: '0.85rem', fontWeight: 600 }}>{selectedDonation.unit}</span>
                </div>
              </div>
            </div>

            {/* Details List */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginBottom: '24px', fontSize: '0.875rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', color: '#4b5563' }}>
                <MapPin size={16} color="#9ca3af" style={{ flexShrink: 0 }} />
                <span><strong>Pickup Location:</strong> {selectedDonation.location || 'Location provided upon request confirmation'}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', color: '#4b5563' }}>
                <Calendar size={16} color="#9ca3af" style={{ flexShrink: 0 }} />
                <span><strong>Availability Deadline:</strong> {formatExpiryTime(selectedDonation.expiryTime)}</span>
              </div>
              {selectedDonation.dietaryTags && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px', color: '#4b5563' }}>
                  <Tag size={16} color="#9ca3af" style={{ flexShrink: 0 }} />
                  <span><strong>Dietary & Allergen Notes:</strong> {selectedDonation.dietaryTags}</span>
                </div>
              )}
            </div>

            {/* Modal Actions */}
            <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end', borderTop: '1px solid #f3f4f6', paddingTop: '16px' }}>
              <button
                type="button"
                onClick={() => setSelectedDonation(null)}
                className="btn btn-outline btn-md"
              >
                Close
              </button>
              <button
                type="button"
                onClick={() => {
                  const target = selectedDonation;
                  setSelectedDonation(null);
                  handleOpenClaimModal(target);
                }}
                className="btn btn-amber btn-md"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <Send size={16} />
                <span>Request Food Portion</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
