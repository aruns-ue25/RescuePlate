import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { donationApi } from '../services/api';
import { 
  Store, 
  Sparkles, 
  PlusCircle, 
  Clock, 
  MapPin, 
  Package, 
  CheckCircle2, 
  AlertCircle, 
  Utensils, 
  Tag, 
  Truck, 
  X, 
  RefreshCw,
  Info,
  Calendar
} from 'lucide-react';

export default function DonorDashboardPage() {
  const { currentUser } = useAuth();

  const [donations, setDonations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState(null);
  const [formSuccess, setFormSuccess] = useState(null);

  // Form State
  const initialFormState = {
    foodTitle: '',
    category: 'Cooked Meals',
    totalQuantity: 20,
    unit: 'portions',
    expiryHours: 4,
    collectionMode: 'Organization Pickup',
    location: currentUser?.location || '',
    notes: '',
    dietaryTags: 'Vegetarian'
  };

  const [formData, setFormData] = useState(initialFormState);

  const fetchMyDonations = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await donationApi.getMyDonations();
      if (res.success && res.data) {
        setDonations(res.data);
      } else {
        setDonations([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to load surplus food listings.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMyDonations();
  }, []);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: name === 'totalQuantity' || name === 'expiryHours' 
        ? (value === '' ? '' : Number(value)) 
        : value
    }));
    if (formError) setFormError(null);
  };

  const handleCreateDonation = async (e) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(null);

    // Client validation matching Acceptance Criteria Scenarios
    if (!formData.foodTitle.trim()) {
      setFormError('Please provide the food item title.');
      return;
    }

    if (!formData.location.trim()) {
      setFormError('Pickup or delivery location is required.');
      return;
    }

    if (formData.totalQuantity <= 0) {
      setFormError('Quantity must be a positive number greater than 0.');
      return;
    }

    if (!formData.expiryHours || formData.expiryHours <= 0) {
      setFormError('Please select a valid availability window / expiry period.');
      return;
    }

    try {
      setSubmitting(true);
      const res = await donationApi.createDonation(formData);
      if (res.success) {
        setFormSuccess('Surplus food donation posted successfully!');
        setFormData(initialFormState);
        fetchMyDonations();
        setTimeout(() => {
          setIsModalOpen(false);
          setFormSuccess(null);
        }, 1500);
      } else {
        setFormError(res.message || 'Failed to create donation listing.');
      }
    } catch (err) {
      setFormError(err.message || err.errors?.[0] || 'An error occurred while posting the donation.');
    } finally {
      setSubmitting(false);
    }
  };

  const formatExpiryTime = (dateStr) => {
    try {
      const date = new Date(dateStr);
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', month: 'short', day: 'numeric' });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="dashboard-page animate-fade-in-up" style={{ minHeight: '80vh', paddingBottom: '80px' }}>
      {/* Top Banner */}
      <div className="dashboard-header-banner donor-theme-banner" style={{ background: 'linear-gradient(135deg, #064e3b 0%, #047857 100%)', color: '#fff', padding: '40px 0' }}>
        <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '20px' }}>
          <div>
            <div className="badge badge-primary" style={{ background: 'rgba(255,255,255,0.2)', color: '#fff', border: 'none', marginBottom: '10px' }}>
              <Store size={14} />
              <span>Donor Microservice Portal</span>
            </div>
            <h1 className="dashboard-title" style={{ color: '#fff', fontSize: '2rem', fontWeight: 800, margin: '4px 0 8px' }}>
              {currentUser?.businessName || currentUser?.name || "Food Donor Dashboard"}
            </h1>
            <p className="dashboard-subtitle" style={{ color: '#d1fae5', margin: 0, fontSize: '1rem', maxWidth: '600px' }}>
              Create surplus food donations, specify portions and expiry periods, and connect with registered charity organizations.
            </p>
          </div>

          <div>
            <button 
              onClick={() => {
                setFormError(null);
                setFormSuccess(null);
                setIsModalOpen(true);
              }}
              className="btn btn-primary"
              style={{
                background: '#10b981',
                color: '#fff',
                border: 'none',
                fontWeight: 700,
                boxShadow: '0 4px 14px rgba(0,0,0,0.25)',
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '12px 24px',
                fontSize: '1rem',
                borderRadius: '10px',
                cursor: 'pointer'
              }}
            >
              <PlusCircle size={20} />
              <span>Post Food Donation</span>
            </button>
          </div>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="container" style={{ marginTop: '36px' }}>
        {/* Metric summary bar */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '20px', marginBottom: '32px' }}>
          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600 }}>Active Listings</div>
            <div style={{ fontSize: '1.75rem', fontWeight: 800, color: '#047857', marginTop: '6px' }}>
              {donations.filter(d => d.status === 'Posted' || d.status === 'Available').length}
            </div>
          </div>
          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600 }}>Total Meals Rescued</div>
            <div style={{ fontSize: '1.75rem', fontWeight: 800, color: '#059669', marginTop: '6px' }}>
              {donations.reduce((acc, curr) => acc + (curr.totalQuantity || 0), 0)}
            </div>
          </div>
          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600 }}>Verified Donor Status</div>
            <div style={{ fontSize: '1.1rem', fontWeight: 700, color: '#10b981', marginTop: '10px', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <CheckCircle2 size={18} /> Active & Verified
            </div>
          </div>
        </div>

        {/* Section Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div>
            <h2 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text-main, #111827)', margin: 0 }}>
              My Surplus Food Listings
            </h2>
            <p style={{ color: '#6b7280', fontSize: '0.9rem', margin: '4px 0 0' }}>
              Real-time status of food donations broadcasted to charity organizations.
            </p>
          </div>
          <button 
            onClick={fetchMyDonations}
            className="btn btn-outline btn-sm"
            style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            title="Refresh Listings"
          >
            <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
            <span>Refresh</span>
          </button>
        </div>

        {/* Listings Grid / Empty State */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: '60px 20px', color: '#6b7280' }}>
            <RefreshCw size={36} className="animate-spin" style={{ margin: '0 auto 16px', color: '#059669' }} />
            <p>Loading your surplus listings...</p>
          </div>
        ) : error ? (
          <div className="alert alert-error" style={{ padding: '20px', borderRadius: '10px', background: '#fef2f2', border: '1px solid #fecaca', color: '#b91c1c', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <AlertCircle size={22} />
            <div>
              <strong>Error fetching donations:</strong> {error}
            </div>
          </div>
        ) : donations.length === 0 ? (
          <div className="profile-card text-center" style={{ padding: '56px 24px', background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: '#ecfdf5', color: '#059669', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px' }}>
              <Utensils size={32} />
            </div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '8px' }}>No Active Food Donations Yet</h3>
            <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 24px', fontSize: '0.95rem' }}>
              Whenever you have surplus cooked meals, bakery goods, or produce, create a donation to make it immediately discoverable by nearby food rescue charities.
            </p>
            <button 
              onClick={() => setIsModalOpen(true)}
              className="btn btn-primary btn-md"
              style={{ display: 'inline-flex', alignItems: 'center', gap: '8px' }}
            >
              <PlusCircle size={18} />
              <span>Create Your First Donation</span>
            </button>
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '22px' }}>
            {donations.map((item) => (
              <div 
                key={item.id}
                className="profile-card"
                style={{
                  borderRadius: '14px',
                  background: '#fff',
                  border: '1px solid #e5e7eb',
                  padding: '22px',
                  boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between'
                }}
              >
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '10px' }}>
                    <span 
                      style={{ 
                        background: '#ecfdf5', 
                        color: '#065f46', 
                        fontSize: '0.75rem', 
                        fontWeight: 700, 
                        padding: '4px 10px', 
                        borderRadius: '20px',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px'
                      }}
                    >
                      {item.category}
                    </span>
                    <span 
                      style={{ 
                        background: item.status === 'Posted' ? '#dbeafe' : '#fef3c7', 
                        color: item.status === 'Posted' ? '#1e40af' : '#92400e', 
                        fontSize: '0.75rem', 
                        fontWeight: 700, 
                        padding: '4px 10px', 
                        borderRadius: '20px'
                      }}
                    >
                      {item.status}
                    </span>
                  </div>

                  <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: '#111827', margin: '0 0 10px', lineHeight: 1.3 }}>
                    {item.foodTitle}
                  </h3>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#059669', fontWeight: 700, fontSize: '1rem', marginBottom: '14px' }}>
                    <Package size={18} />
                    <span>{item.remainingQuantity} / {item.totalQuantity} {item.unit} available</span>
                  </div>

                  <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', color: '#4b5563', fontSize: '0.875rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <Clock size={16} style={{ color: '#ef4444' }} />
                      <span>Expires: <strong>{formatExpiryTime(item.expiryTime)}</strong></span>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <MapPin size={16} style={{ color: '#6b7280' }} />
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{item.location}</span>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <Truck size={16} style={{ color: '#3b82f6' }} />
                      <span>{item.collectionMode}</span>
                    </div>

                    {item.dietaryTags && (
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '2px' }}>
                        <Tag size={16} style={{ color: '#8b5cf6' }} />
                        <span style={{ fontSize: '0.8rem', color: '#6d28d9' }}>{item.dietaryTags}</span>
                      </div>
                    )}

                    {item.notes && (
                      <div style={{ marginTop: '8px', padding: '8px 10px', background: '#f9fafb', borderRadius: '8px', fontSize: '0.8rem', color: '#6b7280', fontStyle: 'italic' }}>
                        "{item.notes}"
                      </div>
                    )}
                  </div>
                </div>

                <div style={{ borderTop: '1px solid #f3f4f6', marginTop: '18px', paddingTop: '12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                    ID: #{item.id} • Posted {new Date(item.createdAt).toLocaleDateString()}
                  </span>
                  <span style={{ fontSize: '0.8rem', fontWeight: 600, color: '#059669' }}>
                    {item.claimedQuantity > 0 ? `${item.claimedQuantity} Claimed` : '0 Claims'}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* CREATE DONATION MODAL */}
      {isModalOpen && (
        <div 
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0, 0, 0, 0.65)',
            backdropFilter: 'blur(4px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 9999,
            padding: '20px'
          }}
        >
          <div 
            className="animate-fade-in-up"
            style={{
              background: '#fff',
              borderRadius: '16px',
              maxWidth: '620px',
              width: '100%',
              maxHeight: '90vh',
              overflowY: 'auto',
              boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.2)',
              padding: '28px'
            }}
          >
            {/* Modal Header */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', borderBottom: '1px solid #e5e7eb', paddingBottom: '14px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <div style={{ background: '#ecfdf5', color: '#059669', width: '38px', height: '38px', borderRadius: '10px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Utensils size={20} />
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 800, color: '#111827' }}>
                    Post Surplus Food Donation
                  </h3>
                  <p style={{ margin: 0, fontSize: '0.85rem', color: '#6b7280' }}>
                    Broadcast surplus meals to local non-profit food charities
                  </p>
                </div>
              </div>
              <button 
                onClick={() => setIsModalOpen(false)}
                style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: '#9ca3af', padding: '6px' }}
              >
                <X size={22} />
              </button>
            </div>

            {/* Error & Success Alerts */}
            {formError && (
              <div style={{ background: '#fef2f2', border: '1px solid #fecaca', color: '#b91c1c', padding: '12px 16px', borderRadius: '8px', marginBottom: '18px', display: 'flex', alignItems: 'center', gap: '10px', fontSize: '0.9rem' }}>
                <AlertCircle size={18} style={{ flexShrink: 0 }} />
                <span>{formError}</span>
              </div>
            )}

            {formSuccess && (
              <div style={{ background: '#ecfdf5', border: '1px solid #a7f3d0', color: '#065f46', padding: '12px 16px', borderRadius: '8px', marginBottom: '18px', display: 'flex', alignItems: 'center', gap: '10px', fontSize: '0.9rem' }}>
                <CheckCircle2 size={18} style={{ flexShrink: 0 }} />
                <span>{formSuccess}</span>
              </div>
            )}

            {/* Donation Form */}
            <form onSubmit={handleCreateDonation} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              {/* Food Title */}
              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Food Item Title <span style={{ color: '#ef4444' }}>*</span>
                </label>
                <input 
                  type="text"
                  name="foodTitle"
                  value={formData.foodTitle}
                  onChange={handleInputChange}
                  placeholder="e.g. Freshly Cooked Rice & Curry Packets"
                  className="form-control"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  required
                />
              </div>

              {/* Category & Unit */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Food Category <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select 
                    name="category"
                    value={formData.category}
                    onChange={handleInputChange}
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', background: '#fff' }}
                    required
                  >
                    <option value="Cooked Meals">Cooked Meals</option>
                    <option value="Bakery">Bakery & Breads</option>
                    <option value="Fresh Produce">Fresh Produce (Fruits/Veg)</option>
                    <option value="Dairy & Chilled">Dairy & Chilled</option>
                    <option value="Packaged Dry">Packaged Dry Goods</option>
                  </select>
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Unit of Measurement <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select 
                    name="unit"
                    value={formData.unit}
                    onChange={handleInputChange}
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', background: '#fff' }}
                    required
                  >
                    <option value="portions">portions / meal boxes</option>
                    <option value="kg">kg (kilograms)</option>
                    <option value="packs">packs / bundles</option>
                    <option value="pieces">pieces / units</option>
                    <option value="trays">catering trays</option>
                  </select>
                </div>
              </div>

              {/* Quantity & Availability Window */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Total Quantity <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input 
                    type="number"
                    name="totalQuantity"
                    min="1"
                    max="10000"
                    value={formData.totalQuantity}
                    onChange={handleInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                    required
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Available Window (Hours) <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select 
                    name="expiryHours"
                    value={formData.expiryHours}
                    onChange={handleInputChange}
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', background: '#fff' }}
                    required
                  >
                    <option value="2">2 Hours (Urgent Hot Food)</option>
                    <option value="4">4 Hours (Same-Day Safe Window)</option>
                    <option value="8">8 Hours (Full Shift)</option>
                    <option value="24">24 Hours (Next Day)</option>
                    <option value="48">48 Hours (Packaged / Bakery)</option>
                    <option value="72">72 Hours (Long Life Produce)</option>
                  </select>
                </div>
              </div>

              {/* Collection Mode & Location */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Handover Collection Mode <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select 
                    name="collectionMode"
                    value={formData.collectionMode}
                    onChange={handleInputChange}
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', background: '#fff' }}
                    required
                  >
                    <option value="Organization Pickup">Organization Pickup</option>
                    <option value="Donor Delivery">Donor Delivery to Center</option>
                  </select>
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Pickup Location Address <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input 
                    type="text"
                    name="location"
                    value={formData.location}
                    onChange={handleInputChange}
                    placeholder="e.g. 45 Galle Road, Colombo 03"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                    required
                  />
                </div>
              </div>

              {/* Dietary Tags */}
              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Dietary / Allergen Tags
                </label>
                <input 
                  type="text"
                  name="dietaryTags"
                  value={formData.dietaryTags}
                  onChange={handleInputChange}
                  placeholder="e.g. Vegetarian, Halal, Nut-Free, Dairy-Free"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                />
              </div>

              {/* Notes */}
              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Handling & Storage Notes
                </label>
                <textarea 
                  name="notes"
                  rows="2"
                  value={formData.notes}
                  onChange={handleInputChange}
                  placeholder="e.g. Packed at 12:30 PM. Keep refrigerated or reheat before serving."
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', resize: 'vertical' }}
                />
              </div>

              {/* Action Buttons */}
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '10px', borderTop: '1px solid #e5e7eb', paddingTop: '16px' }}>
                <button 
                  type="button" 
                  onClick={() => setIsModalOpen(false)}
                  className="btn btn-outline"
                  style={{ padding: '10px 20px', borderRadius: '8px' }}
                  disabled={submitting}
                >
                  Cancel
                </button>
                <button 
                  type="submit" 
                  className="btn btn-primary"
                  style={{
                    background: '#059669',
                    color: '#fff',
                    border: 'none',
                    fontWeight: 700,
                    padding: '10px 24px',
                    borderRadius: '8px',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px',
                    cursor: submitting ? 'not-allowed' : 'pointer'
                  }}
                  disabled={submitting}
                >
                  {submitting ? (
                    <>
                      <RefreshCw size={16} className="animate-spin" />
                      <span>Posting...</span>
                    </>
                  ) : (
                    <>
                      <PlusCircle size={18} />
                      <span>Submit Donation</span>
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

