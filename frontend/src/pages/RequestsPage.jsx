import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { requestApi } from '../services/api';
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
  MessageSquare
} from 'lucide-react';

export default function RequestsPage() {
  const { currentUser } = useAuth();
  const isDonor = currentUser?.role === 'DONOR';
  const isOrg = currentUser?.role === 'ORGANIZATION';

  const [requests, setRequests] = useState([]);
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

  useEffect(() => {
    fetchRequests();
  }, [selectedStatus, currentUser]);

  useEffect(() => {
    if (rejectModalOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [rejectModalOpen]);

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

      if (res && res.data) {
        setRequests(res.data);
      } else {
        setRequests([]);
      }
    } catch (err) {
      setError(err.message || 'Failed to load requests.');
      setRequests([]);
    } finally {
      setLoading(false);
    }
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

  const getStatusBadge = (status) => {
    switch (status?.toUpperCase()) {
      case 'ACCEPTED':
        return (
          <span className="badge badge-emerald" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
            <CheckCircle2 size={13} />
            <span>Accepted</span>
          </span>
        );
      case 'REJECTED':
        return (
          <span className="badge badge-rose" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', background: '#fee2e2', color: '#991b1b' }}>
            <XCircle size={13} />
            <span>Rejected</span>
          </span>
        );
      case 'PENDING':
      default:
        return (
          <span className="badge badge-amber" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
            <Clock size={13} />
            <span>Pending Review</span>
          </span>
        );
    }
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
              ? 'Review, accept, and decline food portion requests submitted by verified charitable organizations.' 
              : 'Track the status and progress of surplus food requests submitted to local food donors.'}
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
            <p style={{ fontWeight: 600 }}>Loading requests...</p>
          </div>
        ) : requests.length === 0 ? (
          /* Scenario 4: Empty State */
          <div style={{ background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb', padding: '60px 24px', textAlign: 'center', boxShadow: '0 2px 10px rgba(0,0,0,0.02)' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: '#f3f4f6', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px' }}>
              {isDonor ? <Inbox size={32} color="#9ca3af" /> : <Send size={32} color="#9ca3af" />}
            </div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', margin: '0 0 8px' }}>
              {isDonor ? 'No Received Requests Found' : 'No Sent Requests Found'}
            </h3>
            <p style={{ color: '#6b7280', maxWidth: '460px', margin: '0 auto 20px', fontSize: '0.925rem' }}>
              {isDonor 
                ? 'You currently have no incoming requests from charities matching the selected filter.'
                : 'Your organization has not submitted any donation requests yet.'}
            </p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {requests.map((req) => (
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
                {/* Left Request Info */}
                <div style={{ flex: '1 1 300px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '8px' }}>
                    {getStatusBadge(req.status)}
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

                  {req.notes && (
                    <div style={{ marginTop: '10px', padding: '8px 12px', background: '#f9fafb', borderRadius: '8px', border: '1px solid #f3f4f6', fontSize: '0.85rem', color: '#4b5563', display: 'flex', gap: '8px', alignItems: 'flex-start' }}>
                      <MessageSquare size={14} style={{ flexShrink: 0, marginTop: '2px', color: '#9ca3af' }} />
                      <span><strong>Notes:</strong> {req.notes}</span>
                    </div>
                  )}

                  {req.status === 'REJECTED' && req.rejectionReason && (
                    <div style={{ marginTop: '10px', padding: '8px 12px', background: '#fef2f2', borderRadius: '8px', border: '1px solid #fee2e2', fontSize: '0.85rem', color: '#991b1b' }}>
                      <strong>Rejection Reason:</strong> {req.rejectionReason}
                    </div>
                  )}
                </div>

                {/* Right Action Buttons (Donor PENDING Actions) */}
                {isDonor && req.status === 'PENDING' && (
                  <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
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
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Decline Reason Modal */}
      {rejectModalOpen && (
        <div className="modal-overlay animate-fade-in" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(4px)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '16px', overflowY: 'auto' }}>
          <div className="modal-card animate-scale-up" style={{ margin: 'auto', background: '#fff', borderRadius: '16px', maxWidth: '480px', width: '100%', padding: '28px', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)' }}>
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
