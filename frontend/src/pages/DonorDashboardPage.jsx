import React, { useState, useEffect } from 'react';
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
  Calendar,
  Search,
  Filter,
  Eye,
  CheckCircle,
  AlertTriangle,
  FileText,
  Edit2,
  Lock,
  Save
} from 'lucide-react';

export default function DonorDashboardPage() {
  const { currentUser } = useAuth();

  const [donations, setDonations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  // Creation Modal State
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState(null);
  const [formSuccess, setFormSuccess] = useState(null);

  // Detail Modal State (Task 3)
  const [selectedDonation, setSelectedDonation] = useState(null);

  // Edit Modal State (Task 4: Edit Donation)
  const [editDonation, setEditDonation] = useState(null);
  const [editFormData, setEditFormData] = useState({});
  const [editSubmitting, setEditSubmitting] = useState(false);
  const [editError, setEditError] = useState(null);
  const [editSuccess, setEditSuccess] = useState(null);

  // Filter & Search States
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [categoryFilter, setCategoryFilter] = useState('ALL');

  // Form State for creating donation
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

  const fetchMyDonations = async (status = statusFilter, search = searchQuery) => {
    try {
      setLoading(true);
      setError(null);
      const params = {};
      if (status && status !== 'ALL') params.status = status;
      if (search && search.trim()) params.search = search.trim();

      const res = await donationApi.getMyDonations(params);
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
    fetchMyDonations(statusFilter, searchQuery);
  }, [statusFilter]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    fetchMyDonations(statusFilter, searchQuery);
  };

  const handleResetFilters = () => {
    setSearchQuery('');
    setStatusFilter('ALL');
    setCategoryFilter('ALL');
    fetchMyDonations('ALL', '');
  };

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

  // Open Edit Modal with pre-filled fields
  const handleOpenEditModal = (item) => {
    const timeLeft = calculateTimeLeft(item.expiryTime);
    if (item.status === 'Completed' || item.status === 'Cancelled' || item.status === 'Fully Claimed' || timeLeft.expired) {
      alert(`This donation is in '${item.status}' state and cannot be modified.`);
      return;
    }

    setEditDonation(item);
    setEditFormData({
      foodTitle: item.foodTitle || '',
      category: item.category || 'Cooked Meals',
      totalQuantity: item.totalQuantity || 1,
      unit: item.unit || 'portions',
      expiryHours: 4,
      collectionMode: item.collectionMode || 'Organization Pickup',
      location: item.location || '',
      notes: item.notes || '',
      dietaryTags: item.dietaryTags || ''
    });
    setEditError(null);
    setEditSuccess(null);
  };

  const handleEditInputChange = (e) => {
    const { name, value } = e.target;
    setEditFormData(prev => ({
      ...prev,
      [name]: name === 'totalQuantity' || name === 'expiryHours' 
        ? (value === '' ? '' : Number(value)) 
        : value
    }));
    if (editError) setEditError(null);
  };

  // Handle Edit Submission (Task 4 Scenarios)
  const handleUpdateDonation = async (e) => {
    e.preventDefault();
    if (!editDonation) return;

    setEditError(null);
    setEditSuccess(null);

    // Scenario 3: Validate updated information
    if (!editFormData.foodTitle?.trim()) {
      setEditError('Food item title cannot be empty.');
      return;
    }

    if (!editFormData.location?.trim()) {
      setEditError('Pickup or delivery location cannot be empty.');
      return;
    }

    // Scenario 4: Validate Quantity
    if (!editFormData.totalQuantity || editFormData.totalQuantity <= 0) {
      setEditError('Total quantity must be a positive number greater than 0.');
      return;
    }

    if (editDonation.claimedQuantity > 0 && editFormData.totalQuantity < editDonation.claimedQuantity) {
      setEditError(`Quantity cannot be reduced below the ${editDonation.claimedQuantity} already claimed ${editDonation.unit}.`);
      return;
    }

    // Scenario 5: Validate Expiry
    if (!editFormData.expiryHours || editFormData.expiryHours <= 0) {
      setEditError('Availability period / expiry hours must be greater than 0.');
      return;
    }

    try {
      setEditSubmitting(true);
      const res = await donationApi.updateDonation(editDonation.id, editFormData);
      if (res.success) {
        setEditSuccess('Donation details updated successfully!');
        fetchMyDonations(statusFilter, searchQuery);
        setTimeout(() => {
          setEditDonation(null);
          setEditSuccess(null);
        }, 1200);
      } else {
        setEditError(res.message || 'Failed to update donation listing.');
      }
    } catch (err) {
      setEditError(err.message || err.errors?.[0] || 'An error occurred while updating the donation.');
    } finally {
      setEditSubmitting(false);
    }
  };

  const handleCreateDonation = async (e) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(null);

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
        fetchMyDonations(statusFilter, searchQuery);
        setTimeout(() => {
          setIsCreateModalOpen(false);
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

  const calculateTimeLeft = (dateStr) => {
    try {
      const diffMs = new Date(dateStr).getTime() - new Date().getTime();
      if (diffMs <= 0) return { expired: true, text: 'Expired' };
      const hours = Math.floor(diffMs / (1000 * 60 * 60));
      const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
      if (hours > 24) {
        const days = Math.floor(hours / 24);
        return { expired: false, text: `${days}d ${hours % 24}h left` };
      }
      return { expired: false, text: `${hours}h ${minutes}m left` };
    } catch {
      return { expired: false, text: '' };
    }
  };

  const getStatusBadgeConfig = (status, expiryTime) => {
    const timeLeft = calculateTimeLeft(expiryTime);
    if (timeLeft.expired && status !== 'Completed' && status !== 'Cancelled') {
      return {
        bg: '#fee2e2',
        color: '#b91c1c',
        border: '#fca5a5',
        label: 'Expired',
        icon: AlertTriangle,
        editable: false
      };
    }

    switch (status) {
      case 'Available':
      case 'Posted':
        return {
          bg: '#ecfdf5',
          color: '#065f46',
          border: '#a7f3d0',
          label: 'Available',
          icon: CheckCircle2,
          editable: true
        };
      case 'Partially Claimed':
        return {
          bg: '#fef3c7',
          color: '#92400e',
          border: '#fde68a',
          label: 'Partially Claimed',
          icon: Clock,
          editable: true
        };
      case 'Fully Claimed':
        return {
          bg: '#ede9fe',
          color: '#5b21b6',
          border: '#ddd6fe',
          label: 'Fully Claimed',
          icon: Package,
          editable: false
        };
      case 'Completed':
        return {
          bg: '#e0f2fe',
          color: '#0369a1',
          border: '#bae6fd',
          label: 'Completed',
          icon: CheckCircle,
          editable: false
        };
      case 'Expired':
        return {
          bg: '#fee2e2',
          color: '#b91c1c',
          border: '#fca5a5',
          label: 'Expired',
          icon: AlertTriangle,
          editable: false
        };
      default:
        return {
          bg: '#f3f4f6',
          color: '#374151',
          border: '#e5e7eb',
          label: status,
          icon: Info,
          editable: false
        };
    }
  };

  const displayedDonations = donations.filter(item => {
    if (categoryFilter !== 'ALL' && item.category.toLowerCase() !== categoryFilter.toLowerCase()) {
      return false;
    }
    return true;
  });

  const totalMealsRescued = donations.reduce((acc, curr) => acc + (curr.totalQuantity || 0), 0);
  const totalClaimedPortions = donations.reduce((acc, curr) => acc + (curr.claimedQuantity || 0), 0);
  const activeListingsCount = donations.filter(d => 
    (d.status === 'Posted' || d.status === 'Available' || d.status === 'Partially Claimed') && 
    !calculateTimeLeft(d.expiryTime).expired
  ).length;

  return (
    <div className="dashboard-page animate-fade-in-up" style={{ minHeight: '80vh', paddingBottom: '80px' }}>
      {/* Top Banner */}
      <div className="dashboard-header-banner donor-theme-banner" style={{ background: 'linear-gradient(135deg, #064e3b 0%, #047857 100%)', color: '#fff', padding: '40px 0' }}>
        <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '20px' }}>
          <div>
            <div className="badge badge-primary" style={{ background: 'rgba(255,255,255,0.2)', color: '#fff', border: 'none', marginBottom: '10px' }}>
              <Store size={14} />
              <span>Donor Food Management Microservice</span>
            </div>
            <h1 className="dashboard-title" style={{ color: '#fff', fontSize: '2rem', fontWeight: 800, margin: '4px 0 8px' }}>
              {currentUser?.businessName || currentUser?.name || "Food Donor Dashboard"}
            </h1>
            <p className="dashboard-subtitle" style={{ color: '#d1fae5', margin: 0, fontSize: '1rem', maxWidth: '650px' }}>
              Track and modify your surplus food listings, monitor remaining portions, view live charity claim updates, and manage your contributions.
            </p>
          </div>

          <div>
            <button 
              onClick={() => {
                setFormError(null);
                setFormSuccess(null);
                setIsCreateModalOpen(true);
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
        {/* Metric Summary Cards */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '20px', marginBottom: '32px' }}>
          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span>Active Listings</span>
              <Utensils size={18} style={{ color: '#10b981' }} />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#047857', marginTop: '6px' }}>
              {activeListingsCount}
            </div>
            <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
              Currently available for charities
            </div>
          </div>

          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span>Total Meals Rescued</span>
              <Package size={18} style={{ color: '#059669' }} />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#059669', marginTop: '6px' }}>
              {totalMealsRescued}
            </div>
            <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
              Portions donated through RescuePlate
            </div>
          </div>

          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span>Portions Claimed</span>
              <CheckCircle2 size={18} style={{ color: '#3b82f6' }} />
            </div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: '#2563eb', marginTop: '6px' }}>
              {totalClaimedPortions}
            </div>
            <div style={{ fontSize: '0.8rem', color: '#9ca3af', marginTop: '4px' }}>
              Portions distributed or in progress
            </div>
          </div>

          <div className="profile-card" style={{ padding: '20px', borderRadius: '12px', background: '#fff', border: '1px solid #e5e7eb', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}>
            <div style={{ color: '#6b7280', fontSize: '0.875rem', fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span>Donor Account</span>
              <CheckCircle2 size={18} style={{ color: '#10b981' }} />
            </div>
            <div style={{ fontSize: '1.15rem', fontWeight: 700, color: '#047857', marginTop: '10px' }}>
              {currentUser?.businessName || currentUser?.name || "Verified Donor"}
            </div>
            <div style={{ fontSize: '0.8rem', color: '#10b981', fontWeight: 600, marginTop: '4px' }}>
              Verified Food Safety Donor
            </div>
          </div>
        </div>

        {/* Section Header with Live Filters & Search */}
        <div style={{ background: '#fff', borderRadius: '14px', border: '1px solid #e5e7eb', padding: '20px', marginBottom: '28px', boxShadow: '0 2px 8px rgba(0,0,0,0.02)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px', marginBottom: '16px' }}>
            <div>
              <h2 style={{ fontSize: '1.35rem', fontWeight: 800, color: '#111827', margin: 0 }}>
                My Surplus Food Listings
              </h2>
              <p style={{ color: '#6b7280', fontSize: '0.875rem', margin: '4px 0 0' }}>
                Manage, edit, and track food donations posted by your establishment.
              </p>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
              <button 
                onClick={() => fetchMyDonations(statusFilter, searchQuery)}
                className="btn btn-outline btn-sm"
                style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
                title="Refresh Listings"
              >
                <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
                <span>Refresh</span>
              </button>
            </div>
          </div>

          {/* Search bar & Status Filter Buttons */}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '14px', alignItems: 'center', justifyContent: 'space-between', borderTop: '1px solid #f3f4f6', paddingTop: '16px' }}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', alignItems: 'center' }}>
              <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#4b5563', marginRight: '4px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                <Filter size={14} /> Status:
              </span>
              {[
                { id: 'ALL', label: 'All Listings' },
                { id: 'Available', label: 'Active / Available' },
                { id: 'Partially Claimed', label: 'Partially Claimed' },
                { id: 'Fully Claimed', label: 'Fully Claimed' },
                { id: 'Expired', label: 'Expired' }
              ].map(chip => (
                <button
                  key={chip.id}
                  onClick={() => setStatusFilter(chip.id)}
                  style={{
                    padding: '6px 14px',
                    borderRadius: '20px',
                    border: '1px solid',
                    borderColor: statusFilter === chip.id ? '#047857' : '#e5e7eb',
                    background: statusFilter === chip.id ? '#ecfdf5' : '#fff',
                    color: statusFilter === chip.id ? '#065f46' : '#4b5563',
                    fontSize: '0.825rem',
                    fontWeight: statusFilter === chip.id ? 700 : 500,
                    cursor: 'pointer',
                    transition: 'all 0.15s ease'
                  }}
                >
                  {chip.label}
                </button>
              ))}
            </div>

            <form onSubmit={handleSearchSubmit} style={{ display: 'flex', alignItems: 'center', gap: '8px', minWidth: '260px' }}>
              <div style={{ position: 'relative', width: '100%' }}>
                <Search size={16} style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }} />
                <input
                  type="text"
                  placeholder="Search by title, location..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '7px 12px 7px 32px',
                    borderRadius: '8px',
                    border: '1px solid #d1d5db',
                    fontSize: '0.875rem'
                  }}
                />
              </div>
              <button 
                type="submit" 
                className="btn btn-primary btn-sm"
                style={{ background: '#047857', border: 'none', whiteSpace: 'nowrap', padding: '7px 14px' }}
              >
                Search
              </button>
            </form>
          </div>
        </div>

        {/* Listings Grid / Empty States */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: '60px 20px', color: '#6b7280' }}>
            <RefreshCw size={36} className="animate-spin" style={{ margin: '0 auto 16px', color: '#059669' }} />
            <p style={{ fontWeight: 600 }}>Loading surplus food listings...</p>
          </div>
        ) : error ? (
          <div className="alert alert-error" style={{ padding: '20px', borderRadius: '10px', background: '#fef2f2', border: '1px solid #fecaca', color: '#b91c1c', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <AlertCircle size={22} />
            <div>
              <strong>Error loading listings:</strong> {error}
            </div>
          </div>
        ) : donations.length === 0 ? (
          <div className="profile-card text-center" style={{ padding: '64px 28px', background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb', boxShadow: '0 4px 14px rgba(0,0,0,0.03)' }}>
            <div style={{ width: '76px', height: '76px', borderRadius: '50%', background: '#ecfdf5', color: '#059669', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 20px' }}>
              <Utensils size={38} />
            </div>
            <h3 style={{ fontSize: '1.35rem', fontWeight: 800, color: '#111827', marginBottom: '10px' }}>
              {statusFilter !== 'ALL' || searchQuery ? 'No Matching Food Donations Found' : 'No Food Donations Created Yet'}
            </h3>
            <p style={{ color: '#6b7280', maxWidth: '480px', margin: '0 auto 28px', fontSize: '0.95rem', lineHeight: 1.5 }}>
              {statusFilter !== 'ALL' || searchQuery 
                ? 'No food listings match your selected filter criteria. Try resetting the filters to view all your donations.'
                : 'As an authenticated food donor, you can make surplus meals, groceries, and bakery items available to registered charity organizations.'}
            </p>

            {statusFilter !== 'ALL' || searchQuery ? (
              <button 
                onClick={handleResetFilters}
                className="btn btn-outline"
                style={{ display: 'inline-flex', alignItems: 'center', gap: '8px', padding: '10px 22px' }}
              >
                <RefreshCw size={16} />
                <span>Reset Filters</span>
              </button>
            ) : (
              <button 
                onClick={() => setIsCreateModalOpen(true)}
                className="btn btn-primary btn-md"
                style={{ 
                  display: 'inline-flex', 
                  alignItems: 'center', 
                  gap: '8px',
                  background: '#047857',
                  border: 'none',
                  padding: '12px 26px',
                  fontSize: '1rem',
                  borderRadius: '10px',
                  boxShadow: '0 4px 12px rgba(4, 120, 87, 0.3)'
                }}
              >
                <PlusCircle size={20} />
                <span>Create Your First Donation</span>
              </button>
            )}
          </div>
        ) : displayedDonations.length === 0 ? (
          <div className="profile-card text-center" style={{ padding: '48px 20px', background: '#fff', borderRadius: '16px', border: '1px solid #e5e7eb' }}>
            <Filter size={32} style={{ color: '#9ca3af', margin: '0 auto 12px' }} />
            <h4 style={{ fontSize: '1.15rem', fontWeight: 700, margin: '0 0 8px' }}>No items in this category</h4>
            <p style={{ color: '#6b7280', fontSize: '0.9rem', marginBottom: '16px' }}>Try switching category or clearing the search query.</p>
            <button onClick={handleResetFilters} className="btn btn-outline btn-sm">Clear Filters</button>
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '24px' }}>
            {displayedDonations.map((item) => {
              const statusConfig = getStatusBadgeConfig(item.status, item.expiryTime);
              const StatusIcon = statusConfig.icon;
              const timeLeft = calculateTimeLeft(item.expiryTime);
              const percentageRemaining = item.totalQuantity > 0 
                ? Math.round((item.remainingQuantity / item.totalQuantity) * 100) 
                : 0;
              const isEditable = statusConfig.editable && !timeLeft.expired;

              return (
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
                    justifyContent: 'space-between',
                    transition: 'transform 0.15s ease, box-shadow 0.15s ease'
                  }}
                >
                  <div>
                    {/* Top Row: Category Tag & Status Badge */}
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
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
                          background: statusConfig.bg, 
                          color: statusConfig.color, 
                          border: `1px solid ${statusConfig.border}`,
                          fontSize: '0.75rem', 
                          fontWeight: 700, 
                          padding: '4px 10px', 
                          borderRadius: '20px',
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '5px'
                        }}
                      >
                        <StatusIcon size={12} />
                        <span>{statusConfig.label}</span>
                      </span>
                    </div>

                    {/* Food Title */}
                    <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: '#111827', margin: '0 0 12px', lineHeight: 1.3 }}>
                      {item.foodTitle}
                    </h3>

                    {/* Remaining Quantity & Progress Bar */}
                    <div style={{ background: '#f9fafb', borderRadius: '10px', padding: '12px', border: '1px solid #f3f4f6', marginBottom: '14px' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                        <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#4b5563', display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <Package size={15} style={{ color: '#047857' }} />
                          Portion Availability
                        </span>
                        <span style={{ fontSize: '0.85rem', fontWeight: 800, color: percentageRemaining === 0 ? '#ef4444' : '#047857' }}>
                          {item.remainingQuantity} / {item.totalQuantity} {item.unit}
                        </span>
                      </div>

                      <div style={{ width: '100%', height: '8px', background: '#e5e7eb', borderRadius: '4px', overflow: 'hidden' }}>
                        <div 
                          style={{ 
                            width: `${percentageRemaining}%`, 
                            height: '100%', 
                            background: percentageRemaining === 0 ? '#ef4444' : percentageRemaining < 25 ? '#f59e0b' : '#10b981',
                            borderRadius: '4px',
                            transition: 'width 0.3s ease'
                          }} 
                        />
                      </div>

                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px', fontSize: '0.75rem', color: '#6b7280' }}>
                        <span>Claimed: <strong>{item.claimedQuantity} {item.unit}</strong></span>
                        <span>{percentageRemaining}% Available</span>
                      </div>
                    </div>

                    {/* Information List */}
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', color: '#4b5563', fontSize: '0.875rem' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <Clock size={16} style={{ color: timeLeft.expired ? '#ef4444' : '#f59e0b', flexShrink: 0 }} />
                        <span style={{ fontSize: '0.85rem' }}>
                          {timeLeft.expired ? (
                            <strong style={{ color: '#ef4444' }}>Expired ({formatExpiryTime(item.expiryTime)})</strong>
                          ) : (
                            <span>Expires: <strong>{formatExpiryTime(item.expiryTime)}</strong> ({timeLeft.text})</span>
                          )}
                        </span>
                      </div>

                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <MapPin size={16} style={{ color: '#6b7280', flexShrink: 0 }} />
                        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', fontSize: '0.85rem' }}>
                          {item.location}
                        </span>
                      </div>

                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <Truck size={16} style={{ color: '#3b82f6', flexShrink: 0 }} />
                        <span style={{ fontSize: '0.85rem' }}>{item.collectionMode}</span>
                      </div>

                      {item.dietaryTags && (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '2px' }}>
                          <Tag size={16} style={{ color: '#8b5cf6', flexShrink: 0 }} />
                          <span style={{ fontSize: '0.8rem', color: '#6d28d9', fontWeight: 600 }}>
                            {item.dietaryTags}
                          </span>
                        </div>
                      )}

                      {item.notes && (
                        <div style={{ marginTop: '6px', padding: '8px 10px', background: '#f9fafb', borderRadius: '8px', fontSize: '0.8rem', color: '#6b7280', fontStyle: 'italic', borderLeft: '3px solid #10b981' }}>
                          "{item.notes}"
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Card Bottom: Metadata & Action Buttons (View Details + Edit) */}
                  <div style={{ borderTop: '1px solid #f3f4f6', marginTop: '18px', paddingTop: '12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '8px' }}>
                    <span style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                      ID #{item.id} {item.updatedAt ? '• Edited' : `• ${new Date(item.createdAt).toLocaleDateString()}`}
                    </span>
                    
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      {/* Scenario 7: Workflow State Guarded Edit Button */}
                      {isEditable ? (
                        <button
                          onClick={() => handleOpenEditModal(item)}
                          className="btn btn-outline btn-sm"
                          style={{ 
                            display: 'flex', 
                            alignItems: 'center', 
                            gap: '4px',
                            borderColor: '#3b82f6',
                            color: '#2563eb',
                            fontSize: '0.8rem',
                            fontWeight: 700,
                            padding: '4px 10px',
                            borderRadius: '6px'
                          }}
                          title="Edit this donation listing"
                        >
                          <Edit2 size={13} />
                          <span>Edit</span>
                        </button>
                      ) : (
                        <button
                          disabled
                          className="btn btn-sm"
                          style={{ 
                            display: 'flex', 
                            alignItems: 'center', 
                            gap: '4px',
                            background: '#f3f4f6',
                            color: '#9ca3af',
                            border: '1px solid #e5e7eb',
                            fontSize: '0.8rem',
                            fontWeight: 600,
                            padding: '4px 10px',
                            borderRadius: '6px',
                            cursor: 'not-allowed'
                          }}
                          title={`Editing restricted: donation is ${statusConfig.label}`}
                        >
                          <Lock size={12} />
                          <span>Locked</span>
                        </button>
                      )}

                      <button
                        onClick={() => setSelectedDonation(item)}
                        className="btn btn-outline btn-sm"
                        style={{ 
                          display: 'flex', 
                          alignItems: 'center', 
                          gap: '4px',
                          borderColor: '#047857',
                          color: '#047857',
                          fontSize: '0.8rem',
                          fontWeight: 700,
                          padding: '4px 10px',
                          borderRadius: '6px'
                        }}
                      >
                        <Eye size={13} />
                        <span>Details</span>
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* EDIT DONATION MODAL (Task 4: Edit Donation) */}
      {editDonation && (
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
                <div style={{ background: '#eff6ff', color: '#2563eb', width: '38px', height: '38px', borderRadius: '10px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Edit2 size={20} />
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 800, color: '#111827' }}>
                    Edit Food Donation #{editDonation.id}
                  </h3>
                  <p style={{ margin: 0, fontSize: '0.85rem', color: '#6b7280' }}>
                    Modify food details, portions, availability, or pickup instructions
                  </p>
                </div>
              </div>
              <button 
                onClick={() => setEditDonation(null)}
                style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: '#9ca3af', padding: '6px' }}
              >
                <X size={22} />
              </button>
            </div>

            {/* Error & Success Alerts */}
            {editError && (
              <div style={{ background: '#fef2f2', border: '1px solid #fecaca', color: '#b91c1c', padding: '12px 16px', borderRadius: '8px', marginBottom: '18px', display: 'flex', alignItems: 'center', gap: '10px', fontSize: '0.9rem' }}>
                <AlertCircle size={18} style={{ flexShrink: 0 }} />
                <span>{editError}</span>
              </div>
            )}

            {editSuccess && (
              <div style={{ background: '#ecfdf5', border: '1px solid #a7f3d0', color: '#065f46', padding: '12px 16px', borderRadius: '8px', marginBottom: '18px', display: 'flex', alignItems: 'center', gap: '10px', fontSize: '0.9rem' }}>
                <CheckCircle2 size={18} style={{ flexShrink: 0 }} />
                <span>{editSuccess}</span>
              </div>
            )}

            {/* Edit Donation Form */}
            <form onSubmit={handleUpdateDonation} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              {/* Food Title */}
              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Food Item Title <span style={{ color: '#ef4444' }}>*</span>
                </label>
                <input 
                  type="text"
                  name="foodTitle"
                  value={editFormData.foodTitle}
                  onChange={handleEditInputChange}
                  className="form-control"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  required
                />
              </div>

              {/* Category & Unit */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Food Category
                  </label>
                  <select
                    name="category"
                    value={editFormData.category}
                    onChange={handleEditInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="Cooked Meals">Cooked Meals</option>
                    <option value="Bakery & Pastries">Bakery & Pastries</option>
                    <option value="Fresh Produce">Fresh Produce</option>
                    <option value="Packaged Foods">Packaged Foods</option>
                    <option value="Beverages">Beverages</option>
                    <option value="Dairy & Eggs">Dairy & Eggs</option>
                  </select>
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Quantity Unit
                  </label>
                  <select
                    name="unit"
                    value={editFormData.unit}
                    onChange={handleEditInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="portions">Portions</option>
                    <option value="kg">Kilograms (kg)</option>
                    <option value="packets">Packets</option>
                    <option value="boxes">Boxes</option>
                    <option value="liters">Liters</option>
                  </select>
                </div>
              </div>

              {/* Quantity & Expiry Extension */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Total Quantity <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input 
                    type="number"
                    name="totalQuantity"
                    min={Math.max(1, editDonation.claimedQuantity)}
                    value={editFormData.totalQuantity}
                    onChange={handleEditInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                    required
                  />
                  {editDonation.claimedQuantity > 0 && (
                    <span style={{ fontSize: '0.75rem', color: '#d97706', marginTop: '4px', display: 'block' }}>
                      * Minimum {editDonation.claimedQuantity} {editDonation.unit} (already claimed)
                    </span>
                  )}
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Extend Expiry (Hours from Now)
                  </label>
                  <select
                    name="expiryHours"
                    value={editFormData.expiryHours}
                    onChange={handleEditInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="2">2 Hours (Immediate Pick up)</option>
                    <option value="4">4 Hours (Standard cooked food)</option>
                    <option value="8">8 Hours (End of day)</option>
                    <option value="24">24 Hours (Next day fresh)</option>
                    <option value="48">48 Hours (Bakery / Produce)</option>
                    <option value="72">72 Hours (Packaged foods)</option>
                  </select>
                </div>
              </div>

              {/* Location & Collection Mode */}
              <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Pickup Location <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input 
                    type="text"
                    name="location"
                    value={editFormData.location}
                    onChange={handleEditInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                    required
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Collection Mode
                  </label>
                  <select
                    name="collectionMode"
                    value={editFormData.collectionMode}
                    onChange={handleEditInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="Organization Pickup">Organization Pickup</option>
                    <option value="Donor Drop-off">Donor Drop-off</option>
                  </select>
                </div>
              </div>

              {/* Dietary Tags */}
              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Dietary Tags / Allergen Info
                </label>
                <input 
                  type="text"
                  name="dietaryTags"
                  value={editFormData.dietaryTags}
                  onChange={handleEditInputChange}
                  className="form-control"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                />
              </div>

              {/* Notes / Handling Instructions */}
              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Handling & Storage Notes
                </label>
                <textarea 
                  name="notes"
                  value={editFormData.notes}
                  onChange={handleEditInputChange}
                  rows="2"
                  className="form-control"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', resize: 'vertical' }}
                />
              </div>

              {/* Modal Buttons */}
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '12px' }}>
                <button 
                  type="button"
                  onClick={() => setEditDonation(null)}
                  className="btn btn-outline"
                  style={{ padding: '10px 20px', borderRadius: '8px' }}
                >
                  Cancel
                </button>
                <button 
                  type="submit"
                  disabled={editSubmitting}
                  className="btn btn-primary"
                  style={{ background: '#2563eb', border: 'none', padding: '10px 24px', borderRadius: '8px', display: 'flex', alignItems: 'center', gap: '8px', fontWeight: 700 }}
                >
                  {editSubmitting ? (
                    <>
                      <RefreshCw size={16} className="animate-spin" />
                      <span>Saving Changes...</span>
                    </>
                  ) : (
                    <>
                      <Save size={18} />
                      <span>Save Changes</span>
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* DETAIL MODAL (Task 3) */}
      {selectedDonation && (
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
              maxWidth: '580px',
              width: '100%',
              maxHeight: '90vh',
              overflowY: 'auto',
              boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.2)',
              padding: '28px'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '20px', borderBottom: '1px solid #e5e7eb', paddingBottom: '14px' }}>
              <div>
                <span 
                  style={{ 
                    background: '#ecfdf5', 
                    color: '#065f46', 
                    fontSize: '0.75rem', 
                    fontWeight: 700, 
                    padding: '3px 10px', 
                    borderRadius: '20px',
                    textTransform: 'uppercase'
                  }}
                >
                  {selectedDonation.category}
                </span>
                <h3 style={{ margin: '8px 0 0', fontSize: '1.35rem', fontWeight: 800, color: '#111827' }}>
                  {selectedDonation.foodTitle}
                </h3>
              </div>
              <button 
                onClick={() => setSelectedDonation(null)}
                style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: '#9ca3af', padding: '6px' }}
              >
                <X size={22} />
              </button>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px', marginBottom: '20px' }}>
              <div style={{ background: '#f9fafb', padding: '14px', borderRadius: '10px', border: '1px solid #e5e7eb' }}>
                <span style={{ fontSize: '0.75rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Current Status</span>
                <div style={{ marginTop: '6px' }}>
                  {(() => {
                    const cfg = getStatusBadgeConfig(selectedDonation.status, selectedDonation.expiryTime);
                    const SIcon = cfg.icon;
                    return (
                      <span style={{ 
                        background: cfg.bg, 
                        color: cfg.color, 
                        border: `1px solid ${cfg.border}`, 
                        padding: '4px 10px', 
                        borderRadius: '20px', 
                        fontSize: '0.85rem', 
                        fontWeight: 700,
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '6px'
                      }}>
                        <SIcon size={14} />
                        {cfg.label}
                      </span>
                    );
                  })()}
                </div>
              </div>

              <div style={{ background: '#f9fafb', padding: '14px', borderRadius: '10px', border: '1px solid #e5e7eb' }}>
                <span style={{ fontSize: '0.75rem', fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Portion Breakdown</span>
                <div style={{ fontSize: '1.1rem', fontWeight: 800, color: '#047857', marginTop: '6px' }}>
                  {selectedDonation.remainingQuantity} <span style={{ fontSize: '0.85rem', fontWeight: 600, color: '#4b5563' }}>remaining of {selectedDonation.totalQuantity} {selectedDonation.unit}</span>
                </div>
              </div>
            </div>

            <div style={{ background: '#fff', border: '1px solid #e5e7eb', borderRadius: '10px', padding: '14px', marginBottom: '20px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem', fontWeight: 700, marginBottom: '8px' }}>
                <span style={{ color: '#047857' }}>Remaining: {selectedDonation.remainingQuantity} {selectedDonation.unit}</span>
                <span style={{ color: '#3b82f6' }}>Claimed: {selectedDonation.claimedQuantity} {selectedDonation.unit}</span>
                <span style={{ color: '#111827' }}>Total: {selectedDonation.totalQuantity} {selectedDonation.unit}</span>
              </div>
              <div style={{ width: '100%', height: '10px', background: '#e5e7eb', borderRadius: '6px', overflow: 'hidden' }}>
                <div 
                  style={{ 
                    width: `${selectedDonation.totalQuantity > 0 ? (selectedDonation.remainingQuantity / selectedDonation.totalQuantity) * 100 : 0}%`, 
                    height: '100%', 
                    background: '#10b981',
                    borderRadius: '6px'
                  }} 
                />
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontSize: '0.9rem', color: '#374151', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                <Clock size={18} style={{ color: '#f59e0b', marginTop: '2px', flexShrink: 0 }} />
                <div>
                  <strong>Expiry / Availability Deadline:</strong>
                  <div style={{ color: '#6b7280', fontSize: '0.85rem', marginTop: '2px' }}>
                    {new Date(selectedDonation.expiryTime).toLocaleString()} ({calculateTimeLeft(selectedDonation.expiryTime).text})
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                <MapPin size={18} style={{ color: '#ef4444', marginTop: '2px', flexShrink: 0 }} />
                <div>
                  <strong>Pickup / Delivery Location:</strong>
                  <div style={{ color: '#6b7280', fontSize: '0.85rem', marginTop: '2px' }}>
                    {selectedDonation.location}
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                <Truck size={18} style={{ color: '#3b82f6', marginTop: '2px', flexShrink: 0 }} />
                <div>
                  <strong>Collection Protocol:</strong>
                  <div style={{ color: '#6b7280', fontSize: '0.85rem', marginTop: '2px' }}>
                    {selectedDonation.collectionMode}
                  </div>
                </div>
              </div>

              {selectedDonation.dietaryTags && (
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                  <Tag size={18} style={{ color: '#8b5cf6', marginTop: '2px', flexShrink: 0 }} />
                  <div>
                    <strong>Dietary Tags:</strong>
                    <div style={{ color: '#6d28d9', fontSize: '0.85rem', fontWeight: 600, marginTop: '2px' }}>
                      {selectedDonation.dietaryTags}
                    </div>
                  </div>
                </div>
              )}

              {selectedDonation.notes && (
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                  <FileText size={18} style={{ color: '#047857', marginTop: '2px', flexShrink: 0 }} />
                  <div>
                    <strong>Donor Notes & Handling Instructions:</strong>
                    <div style={{ color: '#4b5563', fontSize: '0.85rem', marginTop: '2px', fontStyle: 'italic', background: '#f9fafb', padding: '8px 12px', borderRadius: '8px' }}>
                      "{selectedDonation.notes}"
                    </div>
                  </div>
                </div>
              )}
            </div>

            <div style={{ borderTop: '1px solid #e5e7eb', paddingTop: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: '0.8rem', color: '#9ca3af' }}>
                Donation Record #{selectedDonation.id}
              </span>
              <button 
                onClick={() => setSelectedDonation(null)}
                className="btn btn-outline btn-sm"
                style={{ padding: '8px 18px', fontWeight: 600 }}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* CREATE DONATION MODAL (Preserved from Task 2) */}
      {isCreateModalOpen && (
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
                onClick={() => setIsCreateModalOpen(false)}
                style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: '#9ca3af', padding: '6px' }}
              >
                <X size={22} />
              </button>
            </div>

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

            <form onSubmit={handleCreateDonation} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
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

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Food Category <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select
                    name="category"
                    value={formData.category}
                    onChange={handleInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="Cooked Meals">Cooked Meals</option>
                    <option value="Bakery & Pastries">Bakery & Pastries</option>
                    <option value="Fresh Produce">Fresh Produce</option>
                    <option value="Packaged Foods">Packaged Foods</option>
                    <option value="Beverages">Beverages</option>
                    <option value="Dairy & Eggs">Dairy & Eggs</option>
                  </select>
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Quantity Unit
                  </label>
                  <select
                    name="unit"
                    value={formData.unit}
                    onChange={handleInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="portions">Portions</option>
                    <option value="kg">Kilograms (kg)</option>
                    <option value="packets">Packets</option>
                    <option value="boxes">Boxes</option>
                    <option value="liters">Liters</option>
                  </select>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Total Quantity <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input 
                    type="number"
                    name="totalQuantity"
                    min="1"
                    value={formData.totalQuantity}
                    onChange={handleInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                    required
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Expiry Window (Hours) <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select
                    name="expiryHours"
                    value={formData.expiryHours}
                    onChange={handleInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="2">2 Hours (Immediate Pick up)</option>
                    <option value="4">4 Hours (Standard cooked food)</option>
                    <option value="8">8 Hours (End of day)</option>
                    <option value="24">24 Hours (Next day fresh)</option>
                    <option value="48">48 Hours (Bakery / Produce)</option>
                    <option value="72">72 Hours (Packaged foods)</option>
                  </select>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: '14px' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Pickup Location <span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input 
                    type="text"
                    name="location"
                    value={formData.location}
                    onChange={handleInputChange}
                    placeholder="e.g. 123 Galle Road, Colombo 03"
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                    required
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                    Collection Mode
                  </label>
                  <select
                    name="collectionMode"
                    value={formData.collectionMode}
                    onChange={handleInputChange}
                    className="form-control"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                  >
                    <option value="Organization Pickup">Organization Pickup</option>
                    <option value="Donor Drop-off">Donor Drop-off</option>
                  </select>
                </div>
              </div>

              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Dietary Tags / Allergen Info
                </label>
                <input 
                  type="text"
                  name="dietaryTags"
                  value={formData.dietaryTags}
                  onChange={handleInputChange}
                  placeholder="e.g. Vegetarian, Halal, Nut-free"
                  className="form-control"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem' }}
                />
              </div>

              <div>
                <label style={{ display: 'block', fontWeight: 700, fontSize: '0.9rem', marginBottom: '6px', color: '#374151' }}>
                  Handling & Storage Notes
                </label>
                <textarea 
                  name="notes"
                  value={formData.notes}
                  onChange={handleInputChange}
                  rows="2"
                  placeholder="e.g. Packed in insulated containers, please keep refrigerated."
                  className="form-control"
                  style={{ width: '100%', padding: '10px 14px', borderRadius: '8px', border: '1px solid #d1d5db', fontSize: '0.95rem', resize: 'vertical' }}
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '12px' }}>
                <button 
                  type="button"
                  onClick={() => setIsCreateModalOpen(false)}
                  className="btn btn-outline"
                  style={{ padding: '10px 20px', borderRadius: '8px' }}
                >
                  Cancel
                </button>
                <button 
                  type="submit"
                  disabled={submitting}
                  className="btn btn-primary"
                  style={{ background: '#047857', border: 'none', padding: '10px 24px', borderRadius: '8px', display: 'flex', alignItems: 'center', gap: '8px', fontWeight: 700 }}
                >
                  {submitting ? (
                    <>
                      <RefreshCw size={16} className="animate-spin" />
                      <span>Broadcasting...</span>
                    </>
                  ) : (
                    <>
                      <PlusCircle size={18} />
                      <span>Publish Donation</span>
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
