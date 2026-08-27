import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate, Link } from 'react-router-dom';
import { authApi } from '../services/api';
import { 
  User, 
  Building2, 
  Mail, 
  MapPin, 
  Phone, 
  Trash2, 
  LogOut, 
  CheckCircle2, 
  AlertTriangle,
  Save,
  AlertCircle
} from 'lucide-react';

export default function ProfilePage() {
  const { currentUser, logout, updateProfile, deleteAccount } = useAuth();
  const navigate = useNavigate();

  const [profileData, setProfileData] = useState({
    businessOrOrgName: currentUser?.businessName || '',
    contactName: currentUser?.name || '',
    email: currentUser?.email || '',
    phone: '',
    location: '',
    bio: '',
    donorType: 'Restaurant',
    acceptedFoodCategories: []
  });

  const [isEditing, setIsEditing] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState('');
  const [saveError, setSaveError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deletePassword, setDeletePassword] = useState('');
  const [deleteError, setDeleteError] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);

  // Fetch real database profile on mount
  useEffect(() => {
    async function loadProfile() {
      try {
        const res = await authApi.getMyProfile();
        if (res.success && res.data) {
          setProfileData({
            businessOrOrgName: res.data.businessOrOrgName || '',
            contactName: res.data.contactName || '',
            email: res.data.email || currentUser?.email || '',
            phone: res.data.phone || '',
            location: res.data.address || '',
            bio: res.data.bioOrDescription || '',
            donorType: res.data.donorType || 'Restaurant',
            acceptedFoodCategories: res.data.acceptedFoodCategories || []
          });
        }
      } catch (err) {
        // Fallback to current session if network delay
        if (currentUser) {
          setProfileData((prev) => ({
            ...prev,
            businessOrOrgName: currentUser.businessName || '',
            contactName: currentUser.name || '',
            email: currentUser.email || ''
          }));
        }
      }
    }
    if (currentUser) {
      loadProfile();
    }
  }, [currentUser]);

  if (!currentUser) {
    return (
      <div className="container" style={{ padding: '120px 24px', textAlign: 'center' }}>
        <User size={48} className="text-muted" style={{ margin: '0 auto 16px' }} />
        <h2>You are not signed in</h2>
        <p style={{ color: 'var(--text-muted)', marginBottom: '24px' }}>Please sign in to view and manage your RescuePlate profile.</p>
        <Link to="/login" className="btn btn-primary btn-lg">
          <span>Sign In to Your Account</span>
        </Link>
      </div>
    );
  }

  const handleSaveProfile = async (e) => {
    e.preventDefault();
    setSaveError('');
    setSaveSuccess('');
    setIsSaving(true);

    try {
      await updateProfile({
        businessOrOrgName: profileData.businessOrOrgName,
        contactName: profileData.contactName,
        phone: profileData.phone,
        address: profileData.location,
        bioOrDescription: profileData.bio,
        donorType: profileData.donorType,
        acceptedFoodCategories: profileData.acceptedFoodCategories
      });
      setIsEditing(false);
      setSaveSuccess('Profile updated and saved to database successfully!');
      setTimeout(() => setSaveSuccess(''), 5000);
    } catch (err) {
      setSaveError(err.message || 'Failed to update profile in database.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleConfirmDelete = async (e) => {
    e.preventDefault();
    setDeleteError('');

    if (!deletePassword) {
      setDeleteError('Please enter your password to confirm deletion.');
      return;
    }

    setIsDeleting(true);
    try {
      await deleteAccount(deletePassword);
      setShowDeleteModal(false);
      navigate('/');
    } catch (err) {
      setDeleteError(err.message || 'Failed to delete account. Please verify password.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="page-view animate-fade-in-up">
      <div className="page-hero-banner">
        <div className="container text-center">
          <div className="badge badge-primary">
            <User size={14} />
            <span>Account & Organization Profile</span>
          </div>
          <h1 className="page-hero-title">My RescuePlate Profile</h1>
          <p className="page-hero-subtitle">
            Manage your account credentials, business details, and food rescue preferences in PostgreSQL.
          </p>
        </div>
      </div>

      <div className="container" style={{ maxWidth: '800px', marginBottom: '80px' }}>
        {saveSuccess && (
          <div className="auth-success-banner animate-fade-in-up" style={{ marginBottom: '24px' }}>
            <CheckCircle2 size={20} className="text-emerald" />
            <span>{saveSuccess}</span>
          </div>
        )}

        {saveError && (
          <div className="auth-error-banner animate-fade-in-up" style={{ marginBottom: '24px' }}>
            <AlertCircle size={20} className="text-accent" />
            <span>{saveError}</span>
          </div>
        )}

        <div className="profile-card">
          {/* Profile Header */}
          <div className="profile-card-header">
            <div className="profile-avatar-circle">
              {currentUser.role === 'DONOR' ? '🏢' : currentUser.role === 'ORGANIZATION' ? '🤝' : '🛡️'}
            </div>
            <div className="profile-header-info">
              <div className="profile-name-row">
                <h2>{profileData.businessOrOrgName || currentUser.businessName}</h2>
                <span className={`badge ${currentUser.role === 'DONOR' ? 'badge-primary' : currentUser.role === 'ORGANIZATION' ? 'badge-amber' : 'badge-accent'}`}>
                  {currentUser.role} Account
                </span>
              </div>
              <p className="profile-contact-text">{profileData.contactName} • {profileData.email}</p>
            </div>
            <button
              onClick={() => {
                setIsEditing(!isEditing);
                setSaveError('');
              }}
              className="btn btn-outline btn-sm"
              style={{ marginLeft: 'auto' }}
            >
              {isEditing ? 'Cancel Edit' : 'Edit Profile'}
            </button>
          </div>

          {/* Profile Form */}
          <form onSubmit={handleSaveProfile} className="profile-form">
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Business / Organization Name *</label>
                <input
                  type="text"
                  className="form-input"
                  value={profileData.businessOrOrgName}
                  disabled={!isEditing}
                  onChange={(e) => setProfileData({ ...profileData, businessOrOrgName: e.target.value })}
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">Contact Person *</label>
                <input
                  type="text"
                  className="form-input"
                  value={profileData.contactName}
                  disabled={!isEditing}
                  onChange={(e) => setProfileData({ ...profileData, contactName: e.target.value })}
                  required
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Email Address (Registered)</label>
                <input
                  type="email"
                  className="form-input"
                  value={profileData.email}
                  disabled
                />
              </div>

              <div className="form-group">
                <label className="form-label">Phone Number</label>
                <input
                  type="text"
                  className="form-input"
                  value={profileData.phone}
                  disabled={!isEditing}
                  placeholder="+1 (555) 000-0000"
                  onChange={(e) => setProfileData({ ...profileData, phone: e.target.value })}
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Physical Address</label>
              <input
                type="text"
                className="form-input"
                value={profileData.location}
                disabled={!isEditing}
                placeholder="e.g. 124 Central Ave, Metro City"
                onChange={(e) => setProfileData({ ...profileData, location: e.target.value })}
              />
            </div>

            <div className="form-group">
              <label className="form-label">About / Description</label>
              <textarea
                rows={3}
                className="form-input form-textarea"
                value={profileData.bio}
                disabled={!isEditing}
                placeholder="Tell other organizations and donors about your operation..."
                onChange={(e) => setProfileData({ ...profileData, bio: e.target.value })}
              ></textarea>
            </div>

            {isEditing && (
              <button type="submit" disabled={isSaving} className="btn btn-primary btn-lg" style={{ marginTop: '10px' }}>
                <Save size={18} />
                <span>{isSaving ? 'Saving to Database...' : 'Save Profile Changes'}</span>
              </button>
            )}
          </form>

          {/* Quick Action Links */}
          <div className="profile-actions-bar">
            <button
              onClick={() => {
                logout();
                navigate('/');
              }}
              className="btn btn-outline"
            >
              <LogOut size={16} />
              <span>Sign Out</span>
            </button>

            <button
              onClick={() => {
                setShowDeleteModal(true);
                setDeleteError('');
                setDeletePassword('');
              }}
              className="btn btn-ghost text-accent"
              style={{ marginLeft: 'auto' }}
            >
              <Trash2 size={16} />
              <span>Delete Account</span>
            </button>
          </div>
        </div>
      </div>

      {/* Delete Account Modal */}
      {showDeleteModal && (
        <div className="modal-backdrop" onClick={() => setShowDeleteModal(false)}>
          <div className="modal-container" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '480px' }}>
            <div className="modal-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <AlertTriangle size={24} className="text-accent" />
                <h3 className="modal-title">Delete Account?</h3>
              </div>
              <button className="modal-close-btn" onClick={() => setShowDeleteModal(false)}>✕</button>
            </div>

            <p style={{ color: 'var(--text-muted)', fontSize: '0.92rem', marginBottom: '20px' }}>
              Are you sure you want to permanently delete your RescuePlate account from the PostgreSQL database? Once deleted, your account and profile will be permanently removed.
            </p>

            {deleteError && (
              <div className="auth-error-banner" style={{ marginBottom: '16px' }}>
                <span>{deleteError}</span>
              </div>
            )}

            <form onSubmit={handleConfirmDelete} className="modal-form">
              <div className="form-group">
                <label className="form-label">Confirm Your Password *</label>
                <input
                  type="password"
                  className="form-input"
                  placeholder="Enter password to confirm deletion"
                  value={deletePassword}
                  onChange={(e) => setDeletePassword(e.target.value)}
                  required
                />
              </div>

              <div style={{ display: 'flex', gap: '12px', marginTop: '12px' }}>
                <button
                  type="button"
                  onClick={() => setShowDeleteModal(false)}
                  className="btn btn-outline full-width"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isDeleting}
                  className="btn btn-accent full-width"
                >
                  {isDeleting ? 'Deleting...' : 'Delete Permanently'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
