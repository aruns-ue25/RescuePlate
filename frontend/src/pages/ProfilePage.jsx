import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate, Link } from 'react-router-dom';
import { authApi, getProfileImageUrl } from '../services/api';
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
  AlertCircle,
  Camera,
  Upload,
  Loader2,
  RefreshCw
} from 'lucide-react';

export default function ProfilePage() {
  const { currentUser, logout, updateProfile, updateProfilePicture, removeProfilePicture, deleteAccount } = useAuth();
  const navigate = useNavigate();
  const fileInputRef = useRef(null);

  const [profileData, setProfileData] = useState({
    businessOrOrgName: currentUser?.businessName || '',
    contactName: currentUser?.name || '',
    email: currentUser?.email || '',
    phone: '',
    location: '',
    bio: '',
    donorType: 'Restaurant',
    acceptedFoodCategories: [],
    profilePictureUrl: currentUser?.profilePictureUrl || null
  });

  const [isEditing, setIsEditing] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState('');
  const [saveError, setSaveError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  // Profile picture states
  const [isUploadingPic, setIsUploadingPic] = useState(false);
  const [isRemovingPic, setIsRemovingPic] = useState(false);
  const [picSuccess, setPicSuccess] = useState('');
  const [picError, setPicError] = useState('');
  const [showRemovePicModal, setShowRemovePicModal] = useState(false);

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
            acceptedFoodCategories: res.data.acceptedFoodCategories || [],
            profilePictureUrl: res.data.profilePictureUrl || null
          });
        }
      } catch (err) {
        // Fallback to current session if network delay
        if (currentUser) {
          setProfileData((prev) => ({
            ...prev,
            businessOrOrgName: currentUser.businessName || '',
            contactName: currentUser.name || '',
            email: currentUser.email || '',
            profilePictureUrl: currentUser.profilePictureUrl || null
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

  const handleProfilePictureSelect = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setPicError('');
    setPicSuccess('');

    // Scenario 5: File size validation
    const MAX_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB
    if (file.size === 0) {
      setPicError('The selected image file is empty. Please choose a valid image.');
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      setPicError('File size exceeds the 5MB limit. Please choose an image under 5MB.');
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }

    // Scenario 4: File type validation
    const allowedExtensions = ['.jpg', '.jpeg', '.png', '.webp'];
    const fileName = file.name.toLowerCase();
    const hasValidExt = allowedExtensions.some((ext) => fileName.endsWith(ext));
    const allowedMimeTypes = ['image/jpeg', 'image/png', 'image/webp'];
    const hasValidMime = allowedMimeTypes.includes(file.type);

    if (!hasValidExt || !hasValidMime) {
      setPicError('Unsupported file format. Only JPG, PNG, and WEBP formats are supported.');
      if (fileInputRef.current) fileInputRef.current.value = '';
      return;
    }

    setIsUploadingPic(true);
    try {
      const res = await updateProfilePicture(file);
      setProfileData((prev) => ({
        ...prev,
        profilePictureUrl: res.data.profilePictureUrl
      }));
      setPicSuccess(res.message || 'Profile picture updated successfully!');
      setTimeout(() => setPicSuccess(''), 5000);
    } catch (err) {
      setPicError(err.message || 'Failed to upload profile picture.');
    } finally {
      setIsUploadingPic(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleConfirmRemovePic = async () => {
    setIsRemovingPic(true);
    setPicError('');
    setPicSuccess('');
    try {
      const res = await removeProfilePicture();
      setProfileData((prev) => ({
        ...prev,
        profilePictureUrl: null
      }));
      setShowRemovePicModal(false);
      setPicSuccess(res.message || 'Profile picture removed successfully.');
      setTimeout(() => setPicSuccess(''), 5000);
    } catch (err) {
      setPicError(err.message || 'Failed to remove profile picture.');
    } finally {
      setIsRemovingPic(false);
    }
  };

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
            Manage your profile picture, account credentials, and food rescue preferences in PostgreSQL.
          </p>
        </div>
      </div>

      <div className="container" style={{ maxWidth: '820px', marginBottom: '80px' }}>
        {/* Profile Details Notifications */}
        {saveSuccess && (
          <div className="auth-success-banner animate-fade-in-up" style={{ marginBottom: '20px' }}>
            <CheckCircle2 size={20} className="text-emerald" />
            <span>{saveSuccess}</span>
          </div>
        )}

        {saveError && (
          <div className="auth-error-banner animate-fade-in-up" style={{ marginBottom: '20px' }}>
            <AlertCircle size={20} className="text-accent" />
            <span>{saveError}</span>
          </div>
        )}

        {/* Profile Picture Notifications */}
        {picSuccess && (
          <div className="auth-success-banner animate-fade-in-up" style={{ marginBottom: '20px' }}>
            <CheckCircle2 size={20} className="text-emerald" />
            <span>{picSuccess}</span>
          </div>
        )}

        {picError && (
          <div className="auth-error-banner animate-fade-in-up" style={{ marginBottom: '20px' }}>
            <AlertCircle size={20} className="text-accent" />
            <span>{picError}</span>
          </div>
        )}

        <div className="profile-card">
          {/* Profile Header with Avatar Management */}
          <div className="profile-card-header">
            <div className="profile-avatar-wrapper">
              <div className="profile-avatar-circle">
                {profileData.profilePictureUrl ? (
                  <img
                    src={getProfileImageUrl(profileData.profilePictureUrl)}
                    alt="Profile Avatar"
                    className="profile-avatar-img"
                  />
                ) : (
                  <span className="profile-avatar-emoji">
                    {currentUser.role === 'DONOR' ? '🏢' : currentUser.role === 'ORGANIZATION' ? '🤝' : '🛡️'}
                  </span>
                )}
                {isUploadingPic && (
                  <div className="avatar-loading-overlay">
                    <Loader2 className="animate-spin text-white" size={24} />
                  </div>
                )}
              </div>

              <button
                type="button"
                className="avatar-edit-badge"
                onClick={() => fileInputRef.current?.click()}
                title="Upload or change photo"
                disabled={isUploadingPic || isRemovingPic}
              >
                <Camera size={14} />
              </button>
            </div>

            <div className="profile-header-info">
              <div className="profile-name-row">
                <h2>{profileData.businessOrOrgName || currentUser.businessName}</h2>
                <span className={`badge ${currentUser.role === 'DONOR' ? 'badge-primary' : currentUser.role === 'ORGANIZATION' ? 'badge-amber' : 'badge-accent'}`}>
                  {currentUser.role} Account
                </span>
              </div>
              <p className="profile-contact-text">{profileData.contactName} • {profileData.email}</p>

              {/* Photo Management Actions */}
              <div className="profile-photo-actions-row">
                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={handleProfilePictureSelect}
                  accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                  style={{ display: 'none' }}
                />

                <button
                  type="button"
                  className="btn btn-outline btn-xs"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={isUploadingPic || isRemovingPic}
                >
                  {isUploadingPic ? (
                    <>
                      <Loader2 size={13} className="animate-spin" />
                      <span>Uploading...</span>
                    </>
                  ) : profileData.profilePictureUrl ? (
                    <>
                      <RefreshCw size={13} />
                      <span>Change Photo</span>
                    </>
                  ) : (
                    <>
                      <Upload size={13} />
                      <span>Upload Photo</span>
                    </>
                  )}
                </button>

                {profileData.profilePictureUrl && (
                  <button
                    type="button"
                    className="btn btn-ghost btn-xs text-accent"
                    onClick={() => setShowRemovePicModal(true)}
                    disabled={isUploadingPic || isRemovingPic}
                  >
                    <Trash2 size={13} />
                    <span>Remove</span>
                  </button>
                )}

                <span className="photo-hint-text">JPG, PNG, WEBP (Max 5MB)</span>
              </div>
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
                  placeholder="e.g. 0771234567"
                  maxLength={12}
                  onChange={(e) => {
                    const val = e.target.value.replace(/[^0-9+]/g, '');
                    if (val.startsWith('+') ? val.length <= 12 : val.length <= 10) {
                      setProfileData({ ...profileData, phone: val });
                    }
                  }}
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

      {/* Remove Picture Confirmation Modal */}
      {showRemovePicModal && (
        <div className="modal-backdrop" onClick={() => setShowRemovePicModal(false)}>
          <div className="modal-container" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '440px' }}>
            <div className="modal-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <Trash2 size={22} className="text-accent" />
                <h3 className="modal-title">Remove Profile Picture?</h3>
              </div>
              <button className="modal-close-btn" onClick={() => setShowRemovePicModal(false)}>✕</button>
            </div>

            <p style={{ color: 'var(--text-muted)', fontSize: '0.92rem', marginBottom: '22px' }}>
              Are you sure you want to remove your profile photo? Your profile will revert to the default avatar and the image file will be permanently deleted from the server.
            </p>

            <div style={{ display: 'flex', gap: '12px' }}>
              <button
                type="button"
                onClick={() => setShowRemovePicModal(false)}
                className="btn btn-outline full-width"
                disabled={isRemovingPic}
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleConfirmRemovePic}
                disabled={isRemovingPic}
                className="btn btn-accent full-width"
              >
                {isRemovingPic ? (
                  <>
                    <Loader2 size={16} className="animate-spin" />
                    <span>Removing...</span>
                  </>
                ) : (
                  'Remove Photo'
                )}
              </button>
            </div>
          </div>
        </div>
      )}

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
