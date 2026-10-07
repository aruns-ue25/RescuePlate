import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { requestApi, deliveryApi } from '../services/api';
import { 
  Inbox, 
  Send, 
  CheckCircle2, 
  XCircle, 
  Clock, 
  AlertCircle, 
  Filter, 
  Sparkles, 
  Utensils, 
  Building2, 
  Store,
  RefreshCw,
  MessageSquare,
  Truck,
  PackageCheck,
  MapPin,
  Calendar,
  Phone,
  User,
  History,
  Check,
  ChevronRight,
  ShieldCheck
} from 'lucide-react';

export default function RequestsPage() {
  const { currentUser } = useAuth();
  const isDonor = currentUser?.role === 'DONOR';
  const isOrg = currentUser?.role === 'ORGANIZATION';

  const [requests, setRequests] = useState([]);
  const [deliveriesMap, setDeliveriesMap] = useState({}); // { requestId: deliveryTrackingObject }
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [successMsg, setSuccessMsg] = useState(null);

  // Status Filter
  const [selectedStatus, setSelectedStatus] = useState('ALL');

  // Reject Modal state for Donor
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [targetRequestId, setTargetRequestId] = useState(null);
  const [rejectReason, setRejectReason] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  // Arrange Collection Modal State
  const [arrangeModalOpen, setArrangeModalOpen] = useState(false);
  const [arrangeTargetRequest, setArrangeTargetRequest] = useState(null);
  const [arrangeForm, setArrangeForm] = useState({
    pickupAddress: '',
    deliveryAddress: '',
    contactName: '',
    contactPhone: '',
    scheduledCollectionTime: '',
    notes: ''
  });

  // Tracking Timeline Modal State
  const [trackingModalOpen, setTrackingModalOpen] = useState(false);
  const [activeTracking, setActiveTracking] = useState(null);

  // Confirm Action Notes Modal State (for Collect, Receive, Complete)
  const [notesModalOpen, setNotesModalOpen] = useState(false);
  const [notesTargetAction, setNotesTargetAction] = useState(null); // { type: 'collect'|'receive'|'complete', deliveryId: int }
  const [actionNotes, setActionNotes] = useState('');

  useEffect(() => {
    fetchRequests();
  }, [selectedStatus, currentUser]);

  useEffect(() => {
    if (rejectModalOpen || arrangeModalOpen || trackingModalOpen || notesModalOpen) {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }, [rejectModalOpen, arrangeModalOpen, trackingModalOpen, notesModalOpen]);

  useEffect(() => {
    document.body.style.overflow = 'unset';
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, []);

  const fetchRequests = async () => {
    setLoading(true);
    setError(null);
    try {
      let res;
      if (isDonor) {
        res = await requestApi.getReceivedRequests({ status: selectedStatus });
      } else {
        res = await requestApi.getMyRequests({ status: selectedStatus });
      }

      const requestList = (res && res.data) ? res.data : [];
      setRequests(requestList);

      // Fetch delivery tracking for all accepted requests
      const acceptedReqs = requestList.filter(r => r.status?.toUpperCase() === 'ACCEPTED');
      await fetchDeliveriesMap(acceptedReqs);
    } catch (err) {
      setError(err.message || 'Failed to load requests.');
      setRequests([]);
    } finally {
      setLoading(false);
    }
  };

  const getLocalDatetimeLocalString = (date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  const fetchDeliveriesMap = async (acceptedRequests) => {
    const newMap = {};
    await Promise.all(
      acceptedRequests.map(async (req) => {
        try {
          const res = await deliveryApi.getTrackingByRequestId(req.id);
          if (res && res.data) {
            newMap[req.id] = res.data;
          }
        } catch (e) {
          // Only 404 means no arrangement created yet.
          // Non-404 errors (500, network error) indicate service error.
          const isNotFound = e?.status === 404 || e?.response?.status === 404 || (e?.message && e.message.includes('not found'));
          if (isNotFound) {
            newMap[req.id] = null;
          } else {
            console.warn(`Delivery tracking fetch error for request ${req.id}:`, e);
            newMap[req.id] = { _error: true };
          }
        }
      })
    );
    setDeliveriesMap(newMap);
  };

  const handleAcceptRequest = async (requestId) => {
    if (!window.confirm('Are you sure you want to accept this donation request? This will deduct the requested quantity from your available stock.')) {
      return;
    }

    setActionLoading(true);
    setSuccessMsg(null);
    setError(null);

    try {
      const res = await requestApi.acceptRequest(requestId);
      setSuccessMsg(res.message || 'Donation request accepted successfully!');
      fetchRequests();
    } catch (err) {
      setError(err.message || 'Failed to accept donation request.');
    } finally {
      setActionLoading(false);
    }
  };

  const openRejectModal = (requestId) => {
    setTargetRequestId(requestId);
    setRejectReason('');
    setRejectModalOpen(true);
  };

  const handleConfirmReject = async (e) => {
    e.preventDefault();
    if (!targetRequestId) return;

    setActionLoading(true);
    setSuccessMsg(null);
    setError(null);

    try {
      const res = await requestApi.rejectRequest(targetRequestId, rejectReason);
      setSuccessMsg(res.message || 'Donation request rejected.');
      setRejectModalOpen(false);
      setTargetRequestId(null);
      fetchRequests();
    } catch (err) {
      setError(err.message || 'Failed to reject donation request.');
    } finally {
      setActionLoading(false);
    }
  };

  // Open Arrange Collection Modal
  const openArrangeModal = (req) => {
    setArrangeTargetRequest(req);
    // Pre-fill contact details with local time 2 hours in the future
    const defaultTime = getLocalDatetimeLocalString(new Date(Date.now() + 2 * 3600 * 1000));
    setArrangeForm({
      pickupAddress: '',
      deliveryAddress: '',
      contactName: currentUser?.name || currentUser?.businessName || '',
      contactPhone: currentUser?.phone || '',
      scheduledCollectionTime: defaultTime,
      notes: ''
    });
    setArrangeModalOpen(true);
  };

  const handleConfirmArrange = async (e) => {
    e.preventDefault();
    if (!arrangeTargetRequest) return;

    // Validation
    if (!arrangeForm.pickupAddress.trim() || !arrangeForm.deliveryAddress.trim() || !arrangeForm.contactName.trim() || !arrangeForm.contactPhone.trim()) {
      setError('Please fill in all required collection fields.');
      return;
    }

    const scheduledDate = new Date(arrangeForm.scheduledCollectionTime);
    if (isNaN(scheduledDate.getTime()) || scheduledDate <= new Date()) {
      setError('Scheduled Collection Time must be a valid future date and time.');
      return;
    }

    setActionLoading(true);
    setSuccessMsg(null);
    setError(null);

    try {
      const payload = {
        requestId: arrangeTargetRequest.id,
        pickupAddress: arrangeForm.pickupAddress.trim(),
        deliveryAddress: arrangeForm.deliveryAddress.trim(),
        contactName: arrangeForm.contactName.trim(),
        contactPhone: arrangeForm.contactPhone.trim(),
        scheduledCollectionTime: scheduledDate.toISOString(),
        notes: arrangeForm.notes.trim() || null
      };

      const res = await deliveryApi.arrangeCollection(payload);
      setSuccessMsg(res.message || 'Collection arrangement submitted successfully!');
      setArrangeModalOpen(false);
      setArrangeTargetRequest(null);
      fetchRequests();
    } catch (err) {
      setError(err.message || 'Failed to arrange collection.');
    } finally {
      setActionLoading(false);
    }
  };

  // Open Notes Modal for Status Transitions (Collect, Receive, Complete)
  const openNotesModal = (type, deliveryId) => {
    setNotesTargetAction({ type, deliveryId });
    setActionNotes('');
    setNotesModalOpen(true);
  };

  const handleConfirmStatusAction = async (e) => {
    e.preventDefault();
    if (!notesTargetAction) return;

    const { type, deliveryId } = notesTargetAction;
    setActionLoading(true);
    setSuccessMsg(null);
    setError(null);

    try {
      let res;
      if (type === 'collect') {
        res = await deliveryApi.recordCollection(deliveryId, actionNotes);
      } else if (type === 'receive') {
        res = await deliveryApi.confirmReceipt(deliveryId, actionNotes);
      } else if (type === 'complete') {
        res = await deliveryApi.completeDonation(deliveryId, actionNotes);
      }

      setSuccessMsg(res.message || `Delivery status updated successfully!`);
      setNotesModalOpen(false);
      setNotesTargetAction(null);
      fetchRequests();
    } catch (err) {
      setError(err.message || `Failed to update delivery status.`);
    } finally {
      setActionLoading(false);
    }
  };

  // Open Tracking Timeline Modal
  const openTrackingModal = (delivery) => {
    setActiveTracking(delivery);
    setTrackingModalOpen(true);
  };

  const getStatusBadge = (req) => {
    const reqStatus = req.status?.toUpperCase();
    const delivery = deliveriesMap[req.id];

    if (reqStatus === 'REJECTED') {
      return (
        <span className="badge badge-rose" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', background: '#fee2e2', color: '#991b1b' }}>
          <XCircle size={13} />
          <span>Rejected</span>
        </span>
      );
    }

    if (reqStatus === 'PENDING') {
      return (
        <span className="badge badge-amber" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
          <Clock size={13} />
          <span>Pending Review</span>
        </span>
      );
    }

    // Status is ACCEPTED -> Check Delivery Status
    if (delivery) {
      switch (delivery.status) {
        case 'CollectionArranged':
          return (
            <span className="badge" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', background: '#f3e8ff', color: '#6b21a8', border: '1px solid #d8b4fe' }}>
              <Truck size={13} />
              <span>Collection Arranged</span>
            </span>
          );
        case 'Collected':
          return (
            <span className="badge" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', background: '#dbeafe', color: '#1e40af', border: '1px solid #93c5fd' }}>
              <PackageCheck size={13} />
              <span>Collected</span>
            </span>
          );
        case 'Received':
          return (
            <span className="badge" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', background: '#ccfbf1', color: '#0f766e', border: '1px solid #99f6e4' }}>
              <CheckCircle2 size={13} />
              <span>Received</span>
            </span>
          );
        case 'Completed':
          return (
            <span className="badge" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', background: '#dcfce7', color: '#15803d', border: '1px solid #86efac' }}>
              <Sparkles size={13} />
              <span>Completed</span>
            </span>
          );
        default:
          break;
      }
    }

    return (
      <span className="badge badge-emerald" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
        <CheckCircle2 size={13} />
        <span>Request Accepted</span>
      </span>
    );
  };

  return (
    <div className="requests-page animate-fade-in-up">
      {/* Header Banner */}
      <div 
        className="dashboard-header-banner" 
        style={{ 
          background: isDonor 
            ? 'linear-gradient(135deg, #064e3b 0%, #047857 100%)' 
            : 'linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%)', 
          color: '#fff', 
          padding: '40px 0' 
        }}
      >
        <div className="container">
          <div className="badge badge-primary" style={{ background: 'rgba(255,255,255,0.2)', color: '#fff', border: 'none', marginBottom: '10px', display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
            {isDonor ? <Inbox size={14} /> : <Send size={14} />}
            <span>{isDonor ? 'Donor Request Management' : 'Organization Sent Requests'}</span>
          </div>
          <h1 className="dashboard-title" style={{ color: '#fff', fontSize: '2rem', fontWeight: 800, margin: '4px 0 8px' }}>
            {isDonor ? 'Incoming Food Requests' : 'My Sent Food Requests'}
          </h1>
          <p className="dashboard-subtitle" style={{ color: 'rgba(255,255,255,0.9)', margin: 0, fontSize: '1rem', maxWidth: '680px' }}>
            {isDonor 
              ? 'Review, accept, arrange collections, and track food surplus redistribution workflows.' 
              : 'Track surplus food requests, arrange pickup details, confirm receipts, and view delivery history.'}
          </p>
        </div>
      </div>

      <div className="container dashboard-body" style={{ maxWidth: '1100px', margin: '32px auto 80px' }}>
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

        {/* Filters & Refresh Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px', marginBottom: '24px' }}>
          {/* Status Filter Tabs */}
          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            {['ALL', 'PENDING', 'ACCEPTED', 'REJECTED'].map((st) => (
              <button
                key={st}
                onClick={() => setSelectedStatus(st)}
                style={{
                  padding: '8px 16px',
                  borderRadius: '20px',
                  fontSize: '0.875rem',
                  fontWeight: 600,
                  border: '1px solid',
                  borderColor: selectedStatus === st ? '#10b981' : '#e5e7eb',
                  background: selectedStatus === st ? '#10b981' : '#fff',
                  color: selectedStatus === st ? '#fff' : '#4b5563',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease'
                }}
              >
                {st === 'ALL' ? 'All Requests' : st.charAt(0) + st.slice(1).toLowerCase()}
              </button>
            ))}
          </div>

          <button
            onClick={fetchRequests}
            disabled={loading}
            className="btn btn-ghost btn-sm"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: '#6b7280' }}
          >
            <RefreshCw size={15} className={loading ? 'spin-animation' : ''} />
            <span>Refresh</span>
          </button>
        </div>

        {/* Requests List */}
        {loading ? (
          <div style={{ padding: '60px 0', textAlign: 'center', color: '#6b7280' }}>
            <RefreshCw size={28} className="spin-animation" style={{ margin: '0 auto 12px', color: '#10b981' }} />
            <p style={{ fontWeight: 600 }}>Loading requests & delivery workflows...</p>
          </div>
        ) : requests.length === 0 ? (
          <div style={{ background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb', padding: '60px 24px', textAlign: 'center', boxShadow: '0 2px 10px rgba(0,0,0,0.02)' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: '#f3f4f6', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px' }}>
              {isDonor ? <Inbox size={32} color="#9ca3af" /> : <Send size={32} color="#9ca3af" />}
            </div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
              {isDonor ? 'No Received Requests Found' : 'No Sent Requests Found'}
            </h3>
            <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
              {isDonor 
                ? 'You currently have no incoming requests matching the selected filter.'
                : 'Your organization has not submitted any donation requests matching the selected filter.'}
            </p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {requests.map((req) => {
              const rawDelivery = deliveriesMap[req.id];
              const deliveryError = rawDelivery?._error;
              const delivery = rawDelivery && !rawDelivery._error ? rawDelivery : null;
              const isAccepted = req.status?.toUpperCase() === 'ACCEPTED';

              return (
                <div 
                  key={req.id}
                  style={{
                    background: '#fff',
                    borderRadius: '14px',
                    border: '1px solid #e5e7eb',
                    padding: '20px 24px',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.03)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    flexWrap: 'wrap',
                    gap: '20px'
                  }}
                >
                  {/* Left Request & Delivery Info */}
                  <div style={{ flex: '1 1 340px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '8px' }}>
                      {getStatusBadge(req)}
                      <span style={{ fontSize: '0.8rem', color: '#9ca3af' }}>
                        Request #{req.id} &bull; {new Date(req.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>

                    <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: '#111827', margin: '0 0 6px' }}>
                      {req.donationTitle || 'Surplus Food Donation'}
                    </h3>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '16px', fontSize: '0.875rem', color: '#4b5563', flexWrap: 'wrap' }}>
                      <div style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
                        {isDonor ? <Building2 size={15} color="#047857" /> : <Store size={15} color="#d97706" />}
                        <span style={{ fontWeight: 600 }}>
                          {isDonor ? req.organizationName : 'Donor Listing'}
                        </span>
                      </div>

                      <div style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', background: '#f3f4f6', padding: '4px 10px', borderRadius: '6px' }}>
                        <Utensils size={14} color="#6b7280" />
                        <span>
                          Requested: <strong>{req.requestedQuantity} {req.unit}</strong>
                        </span>
                      </div>

                      {req.status === 'ACCEPTED' && req.acceptedQuantity && (
                        <div style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', background: '#ecfdf5', color: '#065f46', padding: '4px 10px', borderRadius: '6px' }}>
                          <CheckCircle2 size={14} />
                          <span>
                            Accepted: <strong>{req.acceptedQuantity} {req.unit}</strong>
                          </span>
                        </div>
                      )}
                    </div>

                    {/* Delivery Quick Details Banner if Arranged */}
                    {delivery && (
                      <div style={{ marginTop: '12px', padding: '10px 14px', background: '#f8fafc', borderRadius: '8px', border: '1px solid #e2e8f0', fontSize: '0.85rem', color: '#334155' }}>
                        <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap', alignItems: 'center' }}>
                          <div style={{ display: 'inline-flex', alignItems: 'center', gap: '5px' }}>
                            <MapPin size={14} color="#6366f1" />
                            <span><strong>Pickup:</strong> {delivery.pickupAddress}</span>
                          </div>
                          <div style={{ display: 'inline-flex', alignItems: 'center', gap: '5px' }}>
                            <Calendar size={14} color="#8b5cf6" />
                            <span><strong>Collection:</strong> {new Date(delivery.scheduledCollectionTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}</span>
                          </div>
                        </div>
                      </div>
                    )}

                    {req.notes && (
                      <div style={{ marginTop: '10px', padding: '8px 12px', background: '#f9fafb', borderRadius: '8px', border: '1px solid #f3f4f6', fontSize: '0.85rem', color: '#4b5563', display: 'flex', gap: '8px', alignItems: 'flex-start' }}>
                        <MessageSquare size={14} style={{ flexShrink: 0, marginTop: '2px', color: '#9ca3af' }} />
                        <span><strong>Notes:</strong> {req.notes}</span>
                      </div>
                    )}
                  </div>

                  {/* Right Action Buttons */}
                  <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                    {/* Donor PENDING Actions */}
                    {isDonor && req.status === 'PENDING' && (
                      <>
                        <button
                          onClick={() => handleAcceptRequest(req.id)}
                          disabled={actionLoading}
                          className="btn btn-primary btn-sm"
                          style={{ background: '#10b981', border: 'none', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 16px' }}
                        >
                          <CheckCircle2 size={16} />
                          <span>Accept Request</span>
                        </button>

                        <button
                          onClick={() => openRejectModal(req.id)}
                          disabled={actionLoading}
                          className="btn btn-outline btn-sm"
                          style={{ color: '#ef4444', borderColor: '#fca5a5', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 16px' }}
                        >
                          <XCircle size={16} />
                          <span>Decline</span>
                        </button>
                      </>
                    )}

                    {/* SPRINT 4 DELIVERY WORKFLOW BUTTONS */}
                    {isAccepted && deliveryError && (
                      <div style={{ fontSize: '0.8rem', color: '#dc2626', background: '#fef2f2', padding: '6px 12px', borderRadius: '6px', border: '1px solid #fee2e2', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600 }}>
                        <AlertCircle size={15} />
                        <span>Delivery service unavailable</span>
                      </div>
                    )}

                    {isAccepted && !delivery && !deliveryError && (
                      <button
                        onClick={() => openArrangeModal(req)}
                        disabled={actionLoading}
                        className="btn btn-sm"
                        style={{ background: 'linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)', color: '#fff', border: 'none', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 16px', boxShadow: '0 2px 6px rgba(124, 58, 237, 0.3)' }}
                      >
                        <Truck size={16} />
                        <span>Arrange Collection</span>
                      </button>
                    )}

                    {delivery && (
                      <>
                        {/* Status: CollectionArranged -> Button: Mark as Collected */}
                        {delivery.status === 'CollectionArranged' && (
                          <button
                            onClick={() => openNotesModal('collect', delivery.id)}
                            disabled={actionLoading}
                            className="btn btn-sm"
                            style={{ background: 'linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%)', color: '#fff', border: 'none', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 16px' }}
                          >
                            <PackageCheck size={16} />
                            <span>Mark as Collected</span>
                          </button>
                        )}

                        {/* Status: Collected -> Organization Button: Confirm Receipt */}
                        {delivery.status === 'Collected' && (
                          isOrg ? (
                            <button
                              onClick={() => openNotesModal('receive', delivery.id)}
                              disabled={actionLoading}
                              className="btn btn-sm"
                              style={{ background: 'linear-gradient(135deg, #0d9488 0%, #0f766e 100%)', color: '#fff', border: 'none', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 16px' }}
                            >
                              <ShieldCheck size={16} />
                              <span>Confirm Receipt</span>
                            </button>
                          ) : (
                            <span style={{ fontSize: '0.8rem', color: '#4b5563', background: '#eff6ff', padding: '6px 12px', borderRadius: '6px', fontStyle: 'italic' }}>
                              Awaiting receipt confirmation by organization
                            </span>
                          )
                        )}

                        {/* Status: Received -> Button: Complete Donation */}
                        {delivery.status === 'Received' && (
                          <button
                            onClick={() => openNotesModal('complete', delivery.id)}
                            disabled={actionLoading}
                            className="btn btn-sm"
                            style={{ background: 'linear-gradient(135deg, #16a34a 0%, #15803d 100%)', color: '#fff', border: 'none', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 16px' }}
                          >
                            <Sparkles size={16} />
                            <span>Complete Donation</span>
                          </button>
                        )}

                        {/* Track Delivery Timeline Button */}
                        <button
                          onClick={() => openTrackingModal(delivery)}
                          className="btn btn-outline btn-sm"
                          style={{ borderColor: '#cbd5e1', color: '#334155', display: 'inline-flex', alignItems: 'center', gap: '6px', fontWeight: 600, padding: '8px 14px' }}
                        >
                          <History size={15} />
                          <span>Track Delivery</span>
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

      {/* 1. Arrange Collection Modal */}
      {arrangeModalOpen && arrangeTargetRequest && (
        <div className="modal-overlay animate-fade-in" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', padding: '60px 1rem 2rem 1rem', overflowY: 'auto' }}>
          <div className="modal-card animate-scale-up" style={{ margin: '0 auto', background: '#fff', borderRadius: '16px', maxWidth: '560px', width: '100%', padding: '28px', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '6px' }}>
              <Truck size={22} color="#7c3aed" />
              <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', margin: 0 }}>
                Arrange Donation Collection
              </h3>
            </div>
            <p style={{ color: '#6b7280', fontSize: '0.875rem', margin: '0 0 20px' }}>
              Enter collection details for <strong>{arrangeTargetRequest.donationTitle}</strong> (Request #{arrangeTargetRequest.id}).
            </p>

            <form onSubmit={handleConfirmArrange}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px', marginBottom: '14px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Pickup Address *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="e.g. 123 Bakery St, Kitchen Entrance"
                    value={arrangeForm.pickupAddress}
                    onChange={(e) => setArrangeForm({ ...arrangeForm, pickupAddress: e.target.value })}
                    required
                  />
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Delivery Address *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="e.g. 456 Shelter Rd, Receiving Dock"
                    value={arrangeForm.deliveryAddress}
                    onChange={(e) => setArrangeForm({ ...arrangeForm, deliveryAddress: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px', marginBottom: '14px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Contact Name *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="Full name of contact person"
                    value={arrangeForm.contactName}
                    onChange={(e) => setArrangeForm({ ...arrangeForm, contactName: e.target.value })}
                    required
                  />
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Contact Phone *</label>
                  <input
                    type="tel"
                    className="form-input"
                    placeholder="e.g. +1 555-0199"
                    value={arrangeForm.contactPhone}
                    onChange={(e) => setArrangeForm({ ...arrangeForm, contactPhone: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div className="form-group" style={{ marginBottom: '14px' }}>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Scheduled Collection Date & Time *</label>
                <input
                  type="datetime-local"
                  className="form-input"
                  value={arrangeForm.scheduledCollectionTime}
                  onChange={(e) => setArrangeForm({ ...arrangeForm, scheduledCollectionTime: e.target.value })}
                  required
                />
              </div>

              <div className="form-group" style={{ marginBottom: '20px' }}>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Collection Instructions / Notes (Optional)</label>
                <textarea
                  className="form-input"
                  rows="2"
                  placeholder="e.g. Ring side doorbell upon arrival; chilled container required."
                  value={arrangeForm.notes}
                  onChange={(e) => setArrangeForm({ ...arrangeForm, notes: e.target.value })}
                  maxLength={500}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button
                  type="button"
                  onClick={() => setArrangeModalOpen(false)}
                  disabled={actionLoading}
                  className="btn btn-ghost btn-sm"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  className="btn btn-primary btn-sm"
                  style={{ background: 'linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)', border: 'none' }}
                >
                  {actionLoading ? 'Saving...' : 'Confirm Arrangement'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 2. Confirm Action Notes Modal (Collect, Receive, Complete) */}
      {notesModalOpen && notesTargetAction && (
        <div className="modal-overlay animate-fade-in" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', padding: '90px 1rem 2rem 1rem', overflowY: 'auto' }}>
          <div className="modal-card animate-scale-up" style={{ margin: '0 auto', background: '#fff', borderRadius: '16px', maxWidth: '460px', width: '100%', padding: '28px', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)' }}>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
              {notesTargetAction.type === 'collect' && 'Record Donation Collection'}
              {notesTargetAction.type === 'receive' && 'Confirm Receipt of Donation'}
              {notesTargetAction.type === 'complete' && 'Complete Donation Lifecycle'}
            </h3>
            <p style={{ color: '#6b7280', fontSize: '0.875rem', margin: '0 0 16px' }}>
              Optionally provide notes for this status update transition.
            </p>

            <form onSubmit={handleConfirmStatusAction}>
              <div className="form-group" style={{ marginBottom: '20px' }}>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Transition Notes (Optional)</label>
                <textarea
                  className="form-input"
                  rows="3"
                  placeholder="e.g. Verified temperature and package count upon transfer."
                  value={actionNotes}
                  onChange={(e) => setActionNotes(e.target.value)}
                  maxLength={500}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button
                  type="button"
                  onClick={() => setNotesModalOpen(false)}
                  disabled={actionLoading}
                  className="btn btn-ghost btn-sm"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  className="btn btn-primary btn-sm"
                  style={{
                    background: notesTargetAction.type === 'collect' ? '#2563eb' : notesTargetAction.type === 'receive' ? '#0d9488' : '#16a34a',
                    border: 'none'
                  }}
                >
                  {actionLoading ? 'Updating...' : 'Submit Status Update'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 3. Delivery Tracking & History Timeline Modal */}
      {trackingModalOpen && activeTracking && (
        <div className="modal-overlay animate-fade-in" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', padding: '50px 1rem 2rem 1rem', overflowY: 'auto' }}>
          <div className="modal-card animate-scale-up" style={{ margin: '0 auto', background: '#fff', borderRadius: '16px', maxWidth: '640px', width: '100%', padding: '28px', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
              <div>
                <span style={{ fontSize: '0.8rem', fontWeight: 600, color: '#6366f1', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Delivery Tracking #{activeTracking.id}
                </span>
                <h3 style={{ fontSize: '1.3rem', fontWeight: 800, color: '#111827', margin: '2px 0 0' }}>
                  {activeTracking.donationTitle}
                </h3>
              </div>
              <button
                onClick={() => setTrackingModalOpen(false)}
                className="btn btn-ghost btn-sm"
                style={{ fontSize: '1.2rem', lineHeight: 1 }}
              >
                &times;
              </button>
            </div>

            {/* Stepper Progress Bar */}
            <div style={{ background: '#f8fafc', padding: '18px', borderRadius: '12px', marginBottom: '20px', border: '1px solid #e2e8f0' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', position: 'relative' }}>
                {[
                  { key: 'CollectionArranged', label: 'Arranged' },
                  { key: 'Collected', label: 'Collected' },
                  { key: 'Received', label: 'Received' },
                  { key: 'Completed', label: 'Completed' }
                ].map((step, idx) => {
                  const statuses = ['CollectionArranged', 'Collected', 'Received', 'Completed'];
                  const currentIdx = statuses.indexOf(activeTracking.status);
                  const isDone = idx <= currentIdx;

                  return (
                    <div key={step.key} style={{ flex: 1, textAlign: 'center', zIndex: 1 }}>
                      <div style={{
                        width: '32px',
                        height: '32px',
                        borderRadius: '50%',
                        background: isDone ? '#10b981' : '#e2e8f0',
                        color: isDone ? '#fff' : '#64748b',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        margin: '0 auto 6px',
                        fontWeight: 700,
                        fontSize: '0.85rem'
                      }}>
                        {isDone ? <Check size={16} /> : idx + 1}
                      </div>
                      <span style={{ fontSize: '0.78rem', fontWeight: isDone ? 700 : 500, color: isDone ? '#065f46' : '#64748b' }}>
                        {step.label}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Details Grid */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '20px', background: '#f9fafb', padding: '14px', borderRadius: '10px', fontSize: '0.85rem' }}>
              <div>
                <span style={{ color: '#6b7280' }}>Pickup Address:</span>
                <p style={{ margin: '2px 0 0', fontWeight: 600, color: '#111827' }}>{activeTracking.pickupAddress}</p>
              </div>
              <div>
                <span style={{ color: '#6b7280' }}>Delivery Address:</span>
                <p style={{ margin: '2px 0 0', fontWeight: 600, color: '#111827' }}>{activeTracking.deliveryAddress}</p>
              </div>
              <div>
                <span style={{ color: '#6b7280' }}>Contact Person:</span>
                <p style={{ margin: '2px 0 0', fontWeight: 600, color: '#111827' }}>{activeTracking.contactName} ({activeTracking.contactPhone})</p>
              </div>
              <div>
                <span style={{ color: '#6b7280' }}>Scheduled Time:</span>
                <p style={{ margin: '2px 0 0', fontWeight: 600, color: '#111827' }}>{new Date(activeTracking.scheduledCollectionTime).toLocaleString()}</p>
              </div>
            </div>

            {/* Status Transition History Timeline */}
            <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: '#111827', marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <History size={16} color="#6366f1" />
              <span>Status Transition Log</span>
            </h4>

            {activeTracking.statusHistory && activeTracking.statusHistory.length > 0 ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', maxHeight: '200px', overflowY: 'auto' }}>
                {activeTracking.statusHistory.map((h) => (
                  <div key={h.id} style={{ padding: '10px 14px', background: '#fff', border: '1px solid #e5e7eb', borderRadius: '8px', fontSize: '0.825rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                      <span style={{ fontWeight: 700, color: '#4f46e5' }}>
                        {h.previousStatus} &rarr; {h.newStatus}
                      </span>
                      <span style={{ color: '#9ca3af' }}>
                        {new Date(h.timestamp).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}
                      </span>
                    </div>
                    <div style={{ color: '#6b7280', display: 'flex', justifyContent: 'space-between' }}>
                      <span>Updated by: <strong>{h.changedByUserId}</strong> ({h.changedByRole})</span>
                    </div>
                    {h.notes && (
                      <p style={{ margin: '4px 0 0', color: '#4b5563', fontStyle: 'italic' }}>
                        "{h.notes}"
                      </p>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <p style={{ fontSize: '0.85rem', color: '#9ca3af', fontStyle: 'italic' }}>No transition history recorded yet.</p>
            )}

            <div style={{ marginTop: '20px', display: 'flex', justifyContent: 'flex-end' }}>
              <button
                onClick={() => setTrackingModalOpen(false)}
                className="btn btn-ghost btn-sm"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Decline Reason Modal for Donor */}
      {rejectModalOpen && (
        <div className="modal-overlay animate-fade-in" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', padding: '90px 1rem 2rem 1rem', overflowY: 'auto' }}>
          <div className="modal-card animate-scale-up" style={{ margin: '0 auto', background: '#fff', borderRadius: '16px', maxWidth: '480px', width: '100%', padding: '28px', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)' }}>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
              Decline Donation Request
            </h3>
            <p style={{ color: '#6b7280', fontSize: '0.875rem', margin: '0 0 16px' }}>
              Optionally provide a short reason for declining this request (e.g. insufficient preparation time or item reserved).
            </p>

            <form onSubmit={handleConfirmReject}>
              <div className="form-group" style={{ marginBottom: '20px' }}>
                <label className="form-label" style={{ fontWeight: 600, fontSize: '0.85rem' }}>Decline Reason (Optional)</label>
                <textarea
                  className="form-input"
                  rows="3"
                  placeholder="e.g. Remaining quantity already committed locally."
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                  maxLength={500}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                <button
                  type="button"
                  onClick={() => setRejectModalOpen(false)}
                  disabled={actionLoading}
                  className="btn btn-ghost btn-sm"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={actionLoading}
                  className="btn btn-primary btn-sm"
                  style={{ background: '#ef4444', border: 'none' }}
                >
                  {actionLoading ? 'Processing...' : 'Confirm Decline'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
