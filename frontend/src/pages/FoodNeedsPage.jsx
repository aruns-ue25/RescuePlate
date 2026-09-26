import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { needRequestApi } from '../services/api';
import { 
  HeartHandshake, 
  PlusCircle, 
  Clock, 
  MapPin, 
  Building2, 
  CheckCircle2, 
  XCircle, 
  AlertCircle, 
  Filter, 
  Search, 
  RefreshCw, 
  Edit3, 
  Ban, 
  Gift, 
  Tag, 
  Sparkles, 
  Calendar,
  ChevronRight,
  X,
  MessageSquare,
  HelpingHand
} from 'lucide-react';

export default function FoodNeedsPage() {
  const { currentUser } = useAuth();
  const isDonor = currentUser?.role === 'DONOR';
  const isOrg = currentUser?.role === 'ORGANIZATION';

  // Active Main Tab
  const [activeTab, setActiveTab] = useState(isOrg ? 'my-needs' : 'browse-needs');

  // Need Requests State
  const [needRequests, setNeedRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [successMsg, setSuccessMsg] = useState(null);

  // Search & Filter
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('ALL');
  const [statusFilter, setStatusFilter] = useState('ALL');

  // Create Need Request Modal State (Org)
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [createForm, setCreateForm] = useState({
    title: '',
    category: 'Cooked Meals',
    quantityNeeded: 10,
    unit: 'portions',
    description: '',
    location: currentUser?.address || '',
    neededByDate: ''
  });
  const [createSubmitting, setCreateSubmitting] = useState(false);
  const [createError, setCreateError] = useState(null);

  // Edit Need Request Modal State (Org)
  const [editingNeed, setEditingNeed] = useState(null);
  const [editForm, setEditForm] = useState({
    title: '',
    category: 'Cooked Meals',
    description: '',
    location: '',
    neededByDate: ''
  });
  const [editSubmitting, setEditSubmitting] = useState(false);
  const [editError, setEditError] = useState(null);

  // Received Offers Modal State (Org)
  const [viewOffersNeed, setViewOffersNeed] = useState(null);
  const [offersList, setOffersList] = useState([]);
  const [offersLoading, setOffersLoading] = useState(false);
  const [offerActionLoading, setOfferActionLoading] = useState(false);

  // Submit Offer Modal State (Donor)
  const [targetOfferNeed, setTargetOfferNeed] = useState(null);
  const [offerForm, setOfferForm] = useState({
    foodType: '',
    offeredQuantity: 1,
    notes: ''
  });
  const [offerSubmitting, setOfferSubmitting] = useState(false);
  const [offerError, setOfferError] = useState(null);

  // My Submitted Offers State (Donor)
  const [myOffers, setMyOffers] = useState([]);
  const [myOffersLoading, setMyOffersLoading] = useState(false);

  const categories = ['ALL', 'Cooked Meals', 'Bakery', 'Fresh Produce', 'Dairy & Chilled', 'Packaged Dry', 'General'];

  useEffect(() => {
    if (activeTab === 'my-needs' && isOrg) {
      fetchMyNeedRequests();
    } else if (activeTab === 'browse-needs') {
      fetchActiveNeedRequests();
    } else if (activeTab === 'my-offers' && isDonor) {
      fetchMySubmittedOffers();
    }
  }, [activeTab, categoryFilter, statusFilter, currentUser]);

  useEffect(() => {
    const isAnyModalOpen = Boolean(isCreateModalOpen || editingNeed || viewOffersNeed || targetOfferNeed);
    if (isAnyModalOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isCreateModalOpen, editingNeed, viewOffersNeed, targetOfferNeed]);

  const fetchMyNeedRequests = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await needRequestApi.getMyNeedRequests({ status: statusFilter });
      if (res && res.data) {
        setNeedRequests(res.data);
      } else {
        setNeedRequests([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to load your food need requests.');
      setNeedRequests([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchActiveNeedRequests = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await needRequestApi.getActiveNeedRequests({
        category: categoryFilter,
        search: searchQuery
      });
      if (res && res.data) {
        setNeedRequests(res.data);
      } else {
        setNeedRequests([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch active community food needs.');
      setNeedRequests([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchMySubmittedOffers = async () => {
    setMyOffersLoading(true);
    setError(null);
    try {
      const res = await needRequestApi.getMySubmittedOffers({ status: statusFilter });
      if (res && res.data) {
        setMyOffers(res.data);
      } else {
        setMyOffers([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch your submitted offers.');
      setMyOffers([]);
    } finally {
      setMyOffersLoading(false);
    }
  };

  // --- Create Need Request Handlers (Org) ---
  const handleCreateNeedSubmit = async (e) => {
    e.preventDefault();
    setCreateSubmitting(true);
    setCreateError(null);

    if (!createForm.neededByDate) {
      setCreateError('Please select a needed-by date and time.');
      setCreateSubmitting(false);
      return;
    }

    if (new Date(createForm.neededByDate) <= new Date()) {
      setCreateError('Needed-by date must be in the future.');
      setCreateSubmitting(false);
      return;
    }

    try {
      const res = await needRequestApi.createNeedRequest({
        title: createForm.title.trim(),
        category: createForm.category,
        quantityNeeded: Number(createForm.quantityNeeded),
        unit: createForm.unit.trim(),
        description: createForm.description.trim(),
        location: createForm.location.trim(),
        neededByDate: createForm.neededByDate
      });

      if (res.success) {
        setSuccessMsg('Food need request raised successfully!');
        setIsCreateModalOpen(false);
        setCreateForm({
          title: '',
          category: 'Cooked Meals',
          quantityNeeded: 10,
          unit: 'portions',
          description: '',
          location: currentUser?.address || '',
          neededByDate: ''
        });
        fetchMyNeedRequests();
      } else {
        setCreateError(res.message || 'Failed to create food need request.');
      }
    } catch (err) {
      setCreateError(err.message || err.errors?.[0] || 'Error creating food need request.');
    } finally {
      setCreateSubmitting(false);
    }
  };

  // --- Edit Need Request Handlers (Org) ---
  const openEditModal = (need) => {
    setEditingNeed(need);
    setEditForm({
      title: need.title,
      category: need.category,
      description: need.description,
      location: need.location,
      neededByDate: need.neededByDate ? new Date(need.neededByDate).toISOString().slice(0, 16) : ''
    });
    setEditError(null);
  };

  const handleEditNeedSubmit = async (e) => {
    e.preventDefault();
    if (!editingNeed) return;

    setEditSubmitting(true);
    setEditError(null);

    if (new Date(editForm.neededByDate) <= new Date()) {
      setEditError('Needed-by date must be in the future.');
      setEditSubmitting(false);
      return;
    }

    try {
      const res = await needRequestApi.updateNeedRequest(editingNeed.id, {
        title: editForm.title.trim(),
        category: editForm.category,
        description: editForm.description.trim(),
        location: editForm.location.trim(),
        neededByDate: editForm.neededByDate
      });

      if (res.success) {
        setSuccessMsg('Food need request updated successfully!');
        setEditingNeed(null);
        fetchMyNeedRequests();
      } else {
        setEditError(res.message || 'Failed to update food need request.');
      }
    } catch (err) {
      setEditError(err.message || err.errors?.[0] || 'Error updating food need request.');
    } finally {
      setEditSubmitting(false);
    }
  };

  // --- Cancel Need Request (Org) ---
  const handleCancelNeed = async (id) => {
    if (!window.confirm('Are you sure you want to cancel this food need request? It will no longer accept donor offers.')) {
      return;
    }

    try {
      const res = await needRequestApi.cancelNeedRequest(id);
      setSuccessMsg('Food need request cancelled.');
      fetchMyNeedRequests();
    } catch (err) {
      setError(err.message || 'Failed to cancel food need request.');
    }
  };

  // --- View Offers Handlers (Org) ---
  const openOffersModal = async (need) => {
    setViewOffersNeed(need);
    setOffersLoading(true);
    try {
      const res = await needRequestApi.getOffersForNeedRequest(need.id);
      if (res && res.data) {
        setOffersList(res.data);
      } else {
        setOffersList([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch offers for this request.');
      setOffersList([]);
    } finally {
      setOffersLoading(false);
    }
  };

  const handleAcceptOffer = async (offerId) => {
    if (!window.confirm('Accept this food offer? The offered quantity will be applied towards fulfilling your need.')) {
      return;
    }

    setOfferActionLoading(true);
    try {
      const res = await needRequestApi.acceptDonorOffer(offerId);
      setSuccessMsg(res.message || 'Food offer accepted!');
      if (viewOffersNeed) {
        openOffersModal(viewOffersNeed);
        fetchMyNeedRequests();
      }
    } catch (err) {
      setError(err.message || 'Failed to accept food offer.');
    } finally {
      setOfferActionLoading(false);
    }
  };

  const handleRejectOffer = async (offerId) => {
    if (!window.confirm('Decline this food offer?')) return;

    setOfferActionLoading(true);
    try {
      const res = await needRequestApi.rejectDonorOffer(offerId);
      setSuccessMsg(res.message || 'Food offer declined.');
      if (viewOffersNeed) {
        openOffersModal(viewOffersNeed);
      }
    } catch (err) {
      setError(err.message || 'Failed to decline food offer.');
    } finally {
      setOfferActionLoading(false);
    }
  };

  // --- Submit Offer Handlers (Donor) ---
  const openSubmitOfferModal = (need) => {
    setTargetOfferNeed(need);
    setOfferForm({
      foodType: '',
      offeredQuantity: Math.min(10, need.remainingNeeded || 10),
      notes: ''
    });
    setOfferError(null);
  };

  const handleOfferSubmit = async (e) => {
    e.preventDefault();
    if (!targetOfferNeed) return;

    setOfferSubmitting(true);
    setOfferError(null);

    if (!offerForm.foodType.trim()) {
      setOfferError('Please specify the food item type you are offering.');
      setOfferSubmitting(false);
      return;
    }

    if (offerForm.offeredQuantity <= 0) {
      setOfferError('Offered quantity must be greater than 0.');
      setOfferSubmitting(false);
      return;
    }

    if (offerForm.offeredQuantity > targetOfferNeed.remainingNeeded) {
      setOfferError(`Offered quantity cannot exceed the remaining needed quantity of ${targetOfferNeed.remainingNeeded} ${targetOfferNeed.unit}.`);
      setOfferSubmitting(false);
      return;
    }

    try {
      const res = await needRequestApi.createDonorOffer(targetOfferNeed.id, {
        foodType: offerForm.foodType.trim(),
        offeredQuantity: Number(offerForm.offeredQuantity),
        notes: offerForm.notes.trim()
      });

      if (res.success) {
        setSuccessMsg(`Food offer submitted to ${targetOfferNeed.organizationName}! Pending organization review.`);
        setTargetOfferNeed(null);
        fetchActiveNeedRequests();
      } else {
        setOfferError(res.message || 'Failed to submit food offer.');
      }
    } catch (err) {
      setOfferError(err.message || err.errors?.[0] || 'Error submitting food offer.');
    } finally {
      setOfferSubmitting(false);
    }
  };

  const calculateHoursLeft = (isoString) => {
    if (!isoString) return { text: 'N/A', urgent: false, expired: false };
    const target = new Date(isoString);
    const now = new Date();
    const diffMs = target - now;
    if (diffMs <= 0) return { text: 'Expired', urgent: true, expired: true };

    const hours = Math.floor(diffMs / (1000 * 60 * 60));
    if (hours < 1) {
      const mins = Math.floor(diffMs / (1000 * 60));
      return { text: `Urgent! Needed in ${mins} mins`, urgent: true, expired: false };
    }
    if (hours < 24) {
      return { text: `Needed in ${hours} hours`, urgent: hours < 6, expired: false };
    }
    const days = Math.floor(hours / 24);
    return { text: `Needed in ${days} days`, urgent: false, expired: false };
  };

  const getStatusBadge = (status) => {
    switch (status?.toUpperCase()) {
      case 'OPEN':
        return <span className="badge badge-primary">Open Needs</span>;
      case 'PARTIALLY_FULFILLED':
        return <span className="badge badge-amber">Partially Fulfilled</span>;
      case 'CLAIMED':
        return <span className="badge badge-emerald">Fully Fulfilled / Claimed</span>;
      case 'CANCELLED':
        return <span className="badge badge-rose" style={{ background: '#f3f4f6', color: '#6b7280' }}>Cancelled</span>;
      case 'EXPIRED':
        return <span className="badge badge-rose" style={{ background: '#fee2e2', color: '#991b1b' }}>Expired</span>;
      default:
        return <span className="badge badge-primary">{status}</span>;
    }
  };

  return (
    <div className="food-needs-page animate-fade-in-up">
      {/* Header Banner */}
      <div 
        className="dashboard-header-banner" 
        style={{ 
          background: 'linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)', 
          color: '#fff', 
          padding: '40px 0' 
        }}
      >
        <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '20px' }}>
          <div>
            <div className="badge badge-primary" style={{ background: 'rgba(255,255,255,0.2)', color: '#fff', border: 'none', marginBottom: '10px', display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
              <HelpingHand size={14} />
              <span>Organization Food Demand Portal</span>
            </div>
            <h1 className="dashboard-title" style={{ color: '#fff', fontSize: '2rem', fontWeight: 800, margin: '4px 0 8px' }}>
              Charity Food Needs & Offers
            </h1>
            <p className="dashboard-subtitle" style={{ color: 'rgba(255,255,255,0.9)', margin: 0, fontSize: '1rem', maxWidth: '680px' }}>
              {isOrg 
                ? 'Raise urgent food requests when your organization requires food, and review incoming offers from verified donors.' 
                : 'Browse urgent food requirements posted by community kitchens and charities, and offer surplus food to help fulfill their needs.'}
            </p>
          </div>

          {isOrg && (
            <button
              onClick={() => {
                setCreateError(null);
                setIsCreateModalOpen(true);
              }}
              className="btn btn-primary"
              style={{
                background: '#10b981',
                color: '#fff',
                border: 'none',
                fontWeight: 700,
                boxShadow: '0 4px 14px rgba(0,0,0,0.2)',
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '12px 22px',
                fontSize: '1rem',
                borderRadius: '10px'
              }}
            >
              <PlusCircle size={18} />
              <span>Raise Food Need</span>
            </button>
          )}
        </div>
      </div>

      <div className="container dashboard-body" style={{ maxWidth: '1150px', margin: '32px auto 80px' }}>
        {/* Banner Alert Messages */}
        {successMsg && (
          <div className="auth-success-banner animate-fade-in-up" style={{ marginBottom: '20px', padding: '14px 18px', background: '#ecfdf5', border: '1px solid #a7f3d0', color: '#065f46', borderRadius: '10px', display: 'flex', alignItems: 'center', gap: '10px' }}>
            <CheckCircle2 size={18} />
            <span>{successMsg}</span>
          </div>
        )}

        {error && (
          <div className="auth-error-banner animate-fade-in-up" style={{ marginBottom: '20px', padding: '14px 18px', background: '#fef2f2', border: '1px solid #fecaca', color: '#991b1b', borderRadius: '10px', display: 'flex', alignItems: 'center', gap: '10px' }}>
            <AlertCircle size={18} />
            <span>{error}</span>
          </div>
        )}

        {/* Navigation Tab Bar */}
        <div style={{ display: 'flex', borderBottom: '2px solid #e5e7eb', marginBottom: '28px', gap: '24px' }}>
          {isOrg && (
            <button
              onClick={() => setActiveTab('my-needs')}
              style={{
                padding: '12px 4px',
                border: 'none',
                background: 'none',
                borderBottom: activeTab === 'my-needs' ? '3px solid #4f46e5' : '3px solid transparent',
                color: activeTab === 'my-needs' ? '#4f46e5' : '#6b7280',
                fontWeight: activeTab === 'my-needs' ? 700 : 600,
                fontSize: '1rem',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '8px'
              }}
            >
              <Building2 size={18} />
              <span>My Raised Food Needs</span>
            </button>
          )}

          <button
            onClick={() => setActiveTab('browse-needs')}
            style={{
              padding: '12px 4px',
              border: 'none',
              background: 'none',
              borderBottom: activeTab === 'browse-needs' ? '3px solid #4f46e5' : '3px solid transparent',
              color: activeTab === 'browse-needs' ? '#4f46e5' : '#6b7280',
              fontWeight: activeTab === 'browse-needs' ? 700 : 600,
              fontSize: '1rem',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}
          >
            <HelpingHand size={18} />
            <span>Browse Active Charity Needs</span>
          </button>

          {isDonor && (
            <button
              onClick={() => setActiveTab('my-offers')}
              style={{
                padding: '12px 4px',
                border: 'none',
                background: 'none',
                borderBottom: activeTab === 'my-offers' ? '3px solid #4f46e5' : '3px solid transparent',
                color: activeTab === 'my-offers' ? '#4f46e5' : '#6b7280',
                fontWeight: activeTab === 'my-offers' ? 700 : 600,
                fontSize: '1rem',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '8px'
              }}
            >
              <Gift size={18} />
              <span>My Submitted Food Offers</span>
            </button>
          )}
        </div>

        {/* --- TAB 1: Organization My Raised Needs --- */}
        {activeTab === 'my-needs' && isOrg && (
          <div>
            {/* Filter pills */}
            <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
              {['ALL', 'OPEN', 'PARTIALLY_FULFILLED', 'CLAIMED', 'CANCELLED', 'EXPIRED'].map((st) => (
                <button
                  key={st}
                  onClick={() => setStatusFilter(st)}
                  style={{
                    padding: '6px 14px',
                    borderRadius: '16px',
                    fontSize: '0.825rem',
                    fontWeight: 600,
                    border: '1px solid',
                    borderColor: statusFilter === st ? '#4f46e5' : '#e5e7eb',
                    background: statusFilter === st ? '#4f46e5' : '#fff',
                    color: statusFilter === st ? '#fff' : '#4b5563',
                    cursor: 'pointer'
                  }}
                >
                  {st === 'ALL' ? 'All Statuses' : st.replace('_', ' ')}
                </button>
              ))}
            </div>

            {loading ? (
              <div style={{ padding: '60px 0', textAlign: 'center', color: '#6b7280' }}>
                <RefreshCw size={28} className="spin-animation" style={{ margin: '0 auto 12px', color: '#4f46e5' }} />
                <p style={{ fontWeight: 600 }}>Loading your raised food needs...</p>
              </div>
            ) : needRequests.length === 0 ? (
              /* Scenario 2: No Requests */
              <div style={{ background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb', padding: '60px 24px', textAlign: 'center' }}>
                <HelpingHand size={40} color="#9ca3af" style={{ margin: '0 auto 16px' }} />
                <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
                  No Food Need Requests Created Yet
                </h3>
                <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
                  When your organization requires specific food items, raise a request so verified local donors can step forward with food offers.
                </p>
                <button
                  onClick={() => setIsCreateModalOpen(true)}
                  className="btn btn-primary"
                  style={{ background: '#4f46e5', border: 'none' }}
                >
                  Raise Your First Food Need
                </button>
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))', gap: '20px' }}>
                {needRequests.map((item) => {
                  const deadlineInfo = calculateHoursLeft(item.neededByDate);
                  const percent = Math.min(100, Math.round((item.fulfilledQuantity / item.quantityNeeded) * 100));

                  return (
                    <div 
                      key={item.id}
                      style={{
                        background: '#fff',
                        borderRadius: '14px',
                        border: '1px solid #e5e7eb',
                        padding: '20px',
                        boxShadow: '0 2px 8px rgba(0,0,0,0.03)',
                        display: 'flex',
                        flexDirection: 'column',
                        justifyContent: 'space-between'
                      }}
                    >
                      <div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                          {getStatusBadge(item.status)}
                          <span style={{ fontSize: '0.78rem', color: deadlineInfo.expired ? '#dc2626' : '#d97706', fontWeight: 600 }}>
                            <Clock size={12} style={{ display: 'inline', marginRight: '4px' }} />
                            {deadlineInfo.text}
                          </span>
                        </div>

                        <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#111827', margin: '0 0 6px' }}>
                          {item.title}
                        </h3>

                        <p style={{ color: '#4b5563', fontSize: '0.875rem', margin: '0 0 14px', lineHeight: 1.4 }}>
                          {item.description}
                        </p>

                        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', fontSize: '0.825rem', color: '#6b7280', marginBottom: '14px' }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                            <MapPin size={14} color="#6b7280" />
                            <span>{item.location}</span>
                          </div>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                            <Calendar size={14} color="#6b7280" />
                            <span>Needed By: <strong>{new Date(item.neededByDate).toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</strong></span>
                          </div>
                        </div>

                        {/* Fulfillment Progress Bar */}
                        <div style={{ background: '#f3f4f6', borderRadius: '8px', padding: '12px', marginBottom: '16px' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.825rem', marginBottom: '6px' }}>
                            <span style={{ color: '#4b5563' }}>Progress: <strong>{item.fulfilledQuantity} / {item.quantityNeeded} {item.unit}</strong></span>
                            <span style={{ color: '#4f46e5', fontWeight: 700 }}>{percent}%</span>
                          </div>
                          <div style={{ width: '100%', height: '8px', background: '#e5e7eb', borderRadius: '4px', overflow: 'hidden' }}>
                            <div style={{ width: `${percent}%`, height: '100%', background: percent === 100 ? '#10b981' : '#4f46e5', transition: 'width 0.3s ease' }}></div>
                          </div>
                        </div>
                      </div>

                      {/* Card Action Buttons */}
                      <div style={{ borderTop: '1px solid #f3f4f6', paddingTop: '14px', display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                        <button
                          onClick={() => openOffersModal(item)}
                          className="btn btn-primary btn-sm"
                          style={{ flex: '1', background: '#4f46e5', border: 'none', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: '6px', fontSize: '0.825rem' }}
                        >
                          <Gift size={14} />
                          <span>Offers ({item.totalOffersCount || 0})</span>
                        </button>

                        {item.status !== 'CANCELLED' && item.status !== 'CLAIMED' && item.status !== 'EXPIRED' && (
                          <>
                            <button
                              onClick={() => openEditModal(item)}
                              className="btn btn-outline btn-sm"
                              style={{ padding: '6px 10px', fontSize: '0.825rem' }}
                              title="Edit Need Request"
                            >
                              <Edit3 size={14} />
                            </button>
                            <button
                              onClick={() => handleCancelNeed(item.id)}
                              className="btn btn-outline btn-sm"
                              style={{ color: '#ef4444', borderColor: '#fca5a5', padding: '6px 10px', fontSize: '0.825rem' }}
                              title="Cancel Need Request"
                            >
                              <Ban size={14} />
                            </button>
                          </>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {/* --- TAB 2: Browse Active Charity Needs (Donors & All Users) --- */}
        {activeTab === 'browse-needs' && (
          <div>
            {/* Search & Category Filter */}
            <div style={{ background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb', padding: '18px', marginBottom: '24px', display: 'flex', gap: '16px', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between' }}>
              <div style={{ display: 'flex', gap: '12px', flex: '1 1 300px', flexWrap: 'wrap' }}>
                <div style={{ position: 'relative', flex: '1 1 200px' }}>
                  <Search size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }} />
                  <input
                    type="text"
                    placeholder="Search charity needs by title, location, or org name..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    style={{ width: '100%', padding: '9px 12px 9px 36px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.875rem' }}
                  />
                </div>

                <select
                  value={categoryFilter}
                  onChange={(e) => setCategoryFilter(e.target.value)}
                  style={{ padding: '9px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.875rem', background: '#fff', cursor: 'pointer' }}
                >
                  {categories.map(cat => <option key={cat} value={cat}>{cat === 'ALL' ? 'All Categories' : cat}</option>)}
                </select>
              </div>

              <button
                onClick={fetchActiveNeedRequests}
                disabled={loading}
                className="btn btn-ghost btn-sm"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: '#6b7280' }}
              >
                <RefreshCw size={15} className={loading ? 'spin-animation' : ''} />
                <span>Search / Refresh</span>
              </button>
            </div>

            {loading ? (
              <div style={{ padding: '60px 0', textAlign: 'center', color: '#6b7280' }}>
                <RefreshCw size={28} className="spin-animation" style={{ margin: '0 auto 12px', color: '#4f46e5' }} />
                <p style={{ fontWeight: 600 }}>Loading active community food needs...</p>
              </div>
            ) : needRequests.length === 0 ? (
              /* Story 3 Scenario 5: No Requests Available */
              <div style={{ background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb', padding: '60px 24px', textAlign: 'center' }}>
                <HelpingHand size={40} color="#9ca3af" style={{ margin: '0 auto 16px' }} />
                <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
                  No Active Food Needs Found
                </h3>
                <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
                  There are currently no active open food needs matching your search. Check back soon!
                </p>
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))', gap: '20px' }}>
                {needRequests.map((item) => {
                  const deadlineInfo = calculateHoursLeft(item.neededByDate);
                  return (
                    <div 
                      key={item.id}
                      style={{
                        background: '#fff',
                        borderRadius: '14px',
                        border: '1px solid #e5e7eb',
                        padding: '20px',
                        boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                        display: 'flex',
                        flexDirection: 'column',
                        justifyContent: 'space-between'
                      }}
                    >
                      <div>
                        {/* Urgency Pill & Category */}
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
                          <span style={{ fontSize: '0.75rem', fontWeight: 700, padding: '3px 10px', borderRadius: '12px', background: '#e0e7ff', color: '#3730a3' }}>
                            {item.category}
                          </span>
                          <span style={{ fontSize: '0.78rem', fontWeight: 700, color: deadlineInfo.urgent ? '#dc2626' : '#d97706', display: 'flex', alignItems: 'center', gap: '4px' }}>
                            <Clock size={13} />
                            {deadlineInfo.text}
                          </span>
                        </div>

                        <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: '#111827', margin: '0 0 6px' }}>
                          {item.title}
                        </h3>

                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#4f46e5', fontSize: '0.85rem', fontWeight: 600, marginBottom: '10px' }}>
                          <Building2 size={15} />
                          <span>{item.organizationName}</span>
                        </div>

                        <p style={{ color: '#4b5563', fontSize: '0.875rem', margin: '0 0 14px', lineHeight: 1.4 }}>
                          {item.description}
                        </p>

                        <div style={{ background: '#f9fafb', borderRadius: '8px', padding: '12px', marginBottom: '16px' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', marginBottom: '4px' }}>
                            <span style={{ color: '#6b7280' }}>Remaining Portion Needed:</span>
                            <strong style={{ color: '#4f46e5', fontSize: '1rem' }}>
                              {item.remainingNeeded} {item.unit}
                            </strong>
                          </div>
                          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem', color: '#6b7280' }}>
                            <span>Location: <strong>{item.location}</strong></span>
                          </div>
                        </div>
                      </div>

                      {/* Donor Action */}
                      <div style={{ borderTop: '1px solid #f3f4f6', paddingTop: '14px' }}>
                        {isDonor ? (
                          <button
                            onClick={() => openSubmitOfferModal(item)}
                            className="btn btn-primary full-width"
                            style={{ background: '#10b981', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontWeight: 700 }}
                          >
                            <Gift size={16} />
                            <span>Offer Food to Charity</span>
                          </button>
                        ) : (
                          <div style={{ fontSize: '0.825rem', color: '#6b7280', textAlign: 'center', padding: '6px' }}>
                            {isOrg ? 'Sign in as a Donor to make a food offer' : 'Sign in to offer food'}
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {/* --- TAB 3: Donor My Submitted Offers --- */}
        {activeTab === 'my-offers' && isDonor && (
          <div>
            {myOffersLoading ? (
              <div style={{ padding: '60px 0', textAlign: 'center', color: '#6b7280' }}>
                <RefreshCw size={28} className="spin-animation" style={{ margin: '0 auto 12px', color: '#10b981' }} />
                <p style={{ fontWeight: 600 }}>Loading your submitted food offers...</p>
              </div>
            ) : myOffers.length === 0 ? (
              <div style={{ background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb', padding: '60px 24px', textAlign: 'center' }}>
                <Gift size={40} color="#9ca3af" style={{ margin: '0 auto 16px' }} />
                <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
                  No Submitted Food Offers Found
                </h3>
                <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
                  Browse active charity food needs and step forward with an offer of surplus food portions.
                </p>
                <button
                  onClick={() => setActiveTab('browse-needs')}
                  className="btn btn-primary"
                  style={{ background: '#10b981', border: 'none' }}
                >
                  Browse Charity Needs
                </button>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
                {myOffers.map((off) => (
                  <div
                    key={off.id}
                    style={{
                      background: '#fff',
                      borderRadius: '12px',
                      border: '1px solid #e5e7eb',
                      padding: '18px 22px',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      flexWrap: 'wrap',
                      gap: '16px'
                    }}
                  >
                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '6px' }}>
                        {off.status === 'ACCEPTED' ? (
                          <span className="badge badge-emerald"><CheckCircle2 size={13} /> Accepted</span>
                        ) : off.status === 'REJECTED' ? (
                          <span className="badge badge-rose" style={{ background: '#fee2e2', color: '#991b1b' }}><XCircle size={13} /> Declined</span>
                        ) : (
                          <span className="badge badge-amber"><Clock size={13} /> Pending Review</span>
                        )}
                        <span style={{ fontSize: '0.8rem', color: '#9ca3af' }}>
                          Offer #{off.id} &bull; {new Date(off.createdAt).toLocaleDateString()}
                        </span>
                      </div>

                      <h4 style={{ fontSize: '1.05rem', fontWeight: 700, color: '#111827', margin: '0 0 4px' }}>
                        Need: {off.orgFoodNeedRequestTitle}
                      </h4>

                      <div style={{ fontSize: '0.875rem', color: '#4b5563' }}>
                        Offered: <strong>{off.offeredQuantity} {off.unit}</strong> of <em>{off.foodType}</em>
                        {off.acceptedQuantity && (
                          <span style={{ color: '#065f46', marginLeft: '10px', fontWeight: 600 }}>
                            (Accepted: {off.acceptedQuantity} {off.unit})
                          </span>
                        )}
                      </div>

                      {off.notes && (
                        <div style={{ fontSize: '0.825rem', color: '#6b7280', marginTop: '6px' }}>
                          <strong>Notes:</strong> {off.notes}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* --- MODAL 1: Create Need Request (Org) --- */}
      {isCreateModalOpen && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '20px', overflowY: 'auto' }}>
          <div style={{ margin: 'auto', background: '#fff', borderRadius: '16px', maxWidth: '520px', width: '100%', padding: '28px', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.2)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h3 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 800, color: '#111827' }}>
                Raise Food Need Request
              </h3>
              <button onClick={() => setIsCreateModalOpen(false)} style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer' }}>
                <X size={20} />
              </button>
            </div>

            {createError && (
              <div style={{ padding: '12px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#991b1b', fontSize: '0.875rem', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <AlertCircle size={16} />
                <span>{createError}</span>
              </div>
            )}

            <form onSubmit={handleCreateNeedSubmit}>
              <div className="form-group" style={{ marginBottom: '14px' }}>
                <label className="form-label">Request Title *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="e.g. Urgent Need for 50 Warm Meal Boxes for Shelter"
                  value={createForm.title}
                  onChange={(e) => setCreateForm({ ...createForm, title: e.target.value })}
                  required
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '14px' }}>
                <div className="form-group">
                  <label className="form-label">Category *</label>
                  <select
                    className="form-input"
                    value={createForm.category}
                    onChange={(e) => setCreateForm({ ...createForm, category: e.target.value })}
                  >
                    {categories.filter(c => c !== 'ALL').map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Quantity Needed *</label>
                  <input
                    type="number"
                    min="1"
                    className="form-input"
                    value={createForm.quantityNeeded}
                    onChange={(e) => setCreateForm({ ...createForm, quantityNeeded: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div className="form-group" style={{ marginBottom: '14px' }}>
                <label className="form-label">Location / Pickup Address *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="e.g. Community Center, 123 Main St"
                  value={createForm.location}
                  onChange={(e) => setCreateForm({ ...createForm, location: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '14px' }}>
                <label className="form-label">Needed By Date & Time *</label>
                <input
                  type="datetime-local"
                  className="form-input"
                  value={createForm.neededByDate}
                  onChange={(e) => setCreateForm({ ...createForm, neededByDate: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '20px' }}>
                <label className="form-label">Detailed Description *</label>
                <textarea
                  rows="3"
                  className="form-input"
                  placeholder="Describe your dietary requirements, target community group, or pickup logistics..."
                  value={createForm.description}
                  onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
                  required
                  maxLength={1000}
                />
              </div>

              <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
                <button type="button" onClick={() => setIsCreateModalOpen(false)} className="btn btn-outline btn-md">Cancel</button>
                <button type="submit" disabled={createSubmitting} className="btn btn-primary btn-md" style={{ background: '#10b981', border: 'none' }}>
                  {createSubmitting ? 'Raising Request...' : 'Publish Food Need'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* --- MODAL 2: Edit Need Request (Org) --- */}
      {editingNeed && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '20px', overflowY: 'auto' }}>
          <div style={{ margin: 'auto', background: '#fff', borderRadius: '16px', maxWidth: '500px', width: '100%', padding: '28px', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.2)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
              <h3 style={{ margin: 0, fontSize: '1.2rem', fontWeight: 800, color: '#111827' }}>
                Edit Food Need Request
              </h3>
              <button onClick={() => setEditingNeed(null)} style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer' }}><X size={20} /></button>
            </div>

            {editError && (
              <div style={{ padding: '10px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#991b1b', fontSize: '0.85rem', marginBottom: '14px' }}>
                {editError}
              </div>
            )}

            <form onSubmit={handleEditNeedSubmit}>
              <div className="form-group" style={{ marginBottom: '12px' }}>
                <label className="form-label">Title *</label>
                <input
                  type="text"
                  className="form-input"
                  value={editForm.title}
                  onChange={(e) => setEditForm({ ...editForm, title: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '12px' }}>
                <label className="form-label">Location *</label>
                <input
                  type="text"
                  className="form-input"
                  value={editForm.location}
                  onChange={(e) => setEditForm({ ...editForm, location: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '12px' }}>
                <label className="form-label">Needed By Date & Time *</label>
                <input
                  type="datetime-local"
                  className="form-input"
                  value={editForm.neededByDate}
                  onChange={(e) => setEditForm({ ...editForm, neededByDate: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '18px' }}>
                <label className="form-label">Description *</label>
                <textarea
                  rows="3"
                  className="form-input"
                  value={editForm.description}
                  onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                  required
                />
              </div>

              <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end' }}>
                <button type="button" onClick={() => setEditingNeed(null)} className="btn btn-ghost btn-sm">Cancel</button>
                <button type="submit" disabled={editSubmitting} className="btn btn-primary btn-sm" style={{ background: '#4f46e5', border: 'none' }}>
                  {editSubmitting ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* --- MODAL 3: View Offers Received (Org) --- */}
      {viewOffersNeed && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '20px', overflowY: 'auto' }}>
          <div style={{ margin: 'auto', background: '#fff', borderRadius: '16px', maxWidth: '600px', width: '100%', padding: '28px', maxHeight: '85vh', overflowY: 'auto', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.2)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
              <div>
                <h3 style={{ margin: 0, fontSize: '1.2rem', fontWeight: 800, color: '#111827' }}>
                  Offers Received
                </h3>
                <span style={{ fontSize: '0.85rem', color: '#6b7280' }}>
                  For: <strong>{viewOffersNeed.title}</strong>
                </span>
              </div>
              <button onClick={() => setViewOffersNeed(null)} style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer' }}><X size={20} /></button>
            </div>

            {offersLoading ? (
              <div style={{ padding: '40px 0', textAlign: 'center', color: '#6b7280' }}>
                <RefreshCw size={24} className="spin-animation" style={{ margin: '0 auto 8px', color: '#4f46e5' }} />
                <p>Loading offers...</p>
              </div>
            ) : offersList.length === 0 ? (
              <div style={{ padding: '40px 0', textAlign: 'center', color: '#6b7280' }}>
                <Gift size={32} color="#9ca3af" style={{ margin: '0 auto 10px' }} />
                <p style={{ margin: 0, fontWeight: 600 }}>No donor offers received yet for this request.</p>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {offersList.map((off) => (
                  <div key={off.id} style={{ background: '#f9fafb', borderRadius: '10px', padding: '14px 18px', border: '1px solid #e5e7eb', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '12px' }}>
                    <div>
                      <div style={{ fontWeight: 700, color: '#111827', fontSize: '0.95rem' }}>
                        {off.donorName}
                      </div>
                      <div style={{ fontSize: '0.85rem', color: '#4b5563', marginTop: '2px' }}>
                        Offered: <strong>{off.offeredQuantity} {off.unit}</strong> of <em>{off.foodType}</em>
                      </div>
                      {off.notes && (
                        <div style={{ fontSize: '0.8rem', color: '#6b7280', marginTop: '4px' }}>
                          Notes: {off.notes}
                        </div>
                      )}
                    </div>

                    <div>
                      {off.status === 'PENDING' ? (
                        <div style={{ display: 'flex', gap: '8px' }}>
                          <button
                            onClick={() => handleAcceptOffer(off.id)}
                            disabled={offerActionLoading}
                            className="btn btn-primary btn-sm"
                            style={{ background: '#10b981', border: 'none', padding: '6px 12px', fontSize: '0.78rem' }}
                          >
                            Accept Offer
                          </button>
                          <button
                            onClick={() => handleRejectOffer(off.id)}
                            disabled={offerActionLoading}
                            className="btn btn-outline btn-sm"
                            style={{ color: '#ef4444', borderColor: '#fca5a5', padding: '6px 12px', fontSize: '0.78rem' }}
                          >
                            Decline
                          </button>
                        </div>
                      ) : (
                        getStatusBadge(off.status)
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* --- MODAL 4: Submit Food Offer (Donor) --- */}
      {targetOfferNeed && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '20px', overflowY: 'auto' }}>
          <div style={{ margin: 'auto', background: '#fff', borderRadius: '16px', maxWidth: '480px', width: '100%', padding: '28px', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.2)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Gift size={20} color="#10b981" />
                <h3 style={{ margin: 0, fontSize: '1.2rem', fontWeight: 800, color: '#111827' }}>
                  Offer Food to Charity
                </h3>
              </div>
              <button onClick={() => setTargetOfferNeed(null)} style={{ background: 'none', border: 'none', color: '#9ca3af', cursor: 'pointer' }}><X size={20} /></button>
            </div>

            {offerError && (
              <div style={{ padding: '10px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#991b1b', fontSize: '0.85rem', marginBottom: '14px' }}>
                {offerError}
              </div>
            )}

            <form onSubmit={handleOfferSubmit}>
              <div style={{ background: '#f9fafb', borderRadius: '8px', padding: '12px', marginBottom: '16px', fontSize: '0.85rem' }}>
                <div>Need Request: <strong>{targetOfferNeed.title}</strong></div>
                <div style={{ color: '#6b7280' }}>Organization: {targetOfferNeed.organizationName}</div>
                <div style={{ color: '#4f46e5', fontWeight: 600, marginTop: '4px' }}>
                  Remaining Needed: {targetOfferNeed.remainingNeeded} {targetOfferNeed.unit}
                </div>
              </div>

              <div className="form-group" style={{ marginBottom: '12px' }}>
                <label className="form-label">Food Item / Type You Are Offering *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="e.g. Cooked Rice & Chicken Curry / Fresh Bread Buns"
                  value={offerForm.foodType}
                  onChange={(e) => setOfferForm({ ...offerForm, foodType: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '12px' }}>
                <label className="form-label">Quantity Offered ({targetOfferNeed.unit}) *</label>
                <input
                  type="number"
                  min="1"
                  max={targetOfferNeed.remainingNeeded}
                  className="form-input"
                  value={offerForm.offeredQuantity}
                  onChange={(e) => setOfferForm({ ...offerForm, offeredQuantity: Number(e.target.value) })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '18px' }}>
                <label className="form-label">Notes / Delivery Details (Optional)</label>
                <textarea
                  rows="2"
                  className="form-input"
                  placeholder="e.g. Prepared 2 hours ago, ready for pickup by 6 PM..."
                  value={offerForm.notes}
                  onChange={(e) => setOfferForm({ ...offerForm, notes: e.target.value })}
                  maxLength={500}
                />
              </div>

              <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end' }}>
                <button type="button" onClick={() => setTargetOfferNeed(null)} className="btn btn-ghost btn-sm">Cancel</button>
                <button type="submit" disabled={offerSubmitting} className="btn btn-primary btn-sm" style={{ background: '#10b981', border: 'none' }}>
                  {offerSubmitting ? 'Submitting...' : 'Submit Food Offer'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
