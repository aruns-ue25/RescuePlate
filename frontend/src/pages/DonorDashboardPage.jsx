import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { Link } from 'react-router-dom';
import { 
  Store, 
  PlusCircle, 
  Package, 
  Clock, 
  MapPin, 
  Truck, 
  CheckCircle2, 
  AlertCircle, 
  TrendingUp, 
  Sparkles,
  Layers,
  ArrowRight,
  ShieldCheck
} from 'lucide-react';

export default function DonorDashboardPage() {
  const { currentUser } = useAuth();

  // Active listings state
  const [donations, setDonations] = useState([
    {
      id: "DON-101",
      foodTitle: "Artisan Sourdough & Fresh Croissants",
      category: "Bakery",
      totalQuantity: 100,
      claimedQuantity: 35,
      remainingQuantity: 65,
      unit: "portions",
      expiryTime: "Today at 9:00 PM (3h remaining)",
      collectionMode: "Organization Pickup",
      status: "Partially Claimed",
      location: "124 Central Ave",
      notes: "Packed fresh in food-grade thermal boxes."
    },
    {
      id: "DON-102",
      foodTitle: "Gourmet Buffet Surplus - Roast Veggies & Rice",
      category: "Cooked Meals",
      totalQuantity: 60,
      claimedQuantity: 60,
      remainingQuantity: 0,
      unit: "meal boxes",
      expiryTime: "Completed",
      collectionMode: "Donor Delivery",
      status: "Completed",
      location: "Grand Azure Hotel",
      notes: "Received and confirmed by Hope Community Center."
    }
  ]);

  // New Post Form State
  const [showPostModal, setShowPostModal] = useState(false);
  const [newPost, setNewPost] = useState({
    foodTitle: '',
    category: 'Cooked Meals',
    quantity: 50,
    unit: 'portions',
    hoursAvailable: 4,
    collectionMode: 'Organization Pickup',
    location: currentUser?.businessName ? 'Current Business Location' : '124 Central Ave',
    notes: ''
  });

  const [toastMsg, setToastMsg] = useState('');

  const handleCreateDonation = (e) => {
    e.preventDefault();
    if (!newPost.foodTitle || !newPost.quantity) return;

    const createdItem = {
      id: "DON-" + (100 + donations.length + 1),
      foodTitle: newPost.foodTitle,
      category: newPost.category,
      totalQuantity: Number(newPost.quantity),
      claimedQuantity: 0,
      remainingQuantity: Number(newPost.quantity),
      unit: newPost.unit,
      expiryTime: `${newPost.hoursAvailable} hours remaining`,
      collectionMode: newPost.collectionMode,
      status: "Posted",
      location: newPost.location,
      notes: newPost.notes || "Prepared in accordance with food hygiene standards."
    };

    setDonations([createdItem, ...donations]);
    setShowPostModal(false);
    setToastMsg(`Surplus listing "${newPost.foodTitle}" posted successfully!`);
    setTimeout(() => setToastMsg(''), 4500);

    setNewPost({
      foodTitle: '',
      category: 'Cooked Meals',
      quantity: 50,
      unit: 'portions',
      hoursAvailable: 4,
      collectionMode: 'Organization Pickup',
      location: '124 Central Ave',
      notes: ''
    });
  };

  // Role Gate Warning if not a donor
  const isDonor = currentUser?.role === 'DONOR' || !currentUser;

  return (
    <div className="dashboard-page animate-fade-in-up">
      {/* Top Header */}
      <div className="dashboard-header-banner donor-theme-banner">
        <div className="container">
          <div className="dashboard-header-flex">
            <div>
              <div className="badge badge-primary">
                <Store size={14} />
                <span>Food Donor Portal</span>
              </div>
              <h1 className="dashboard-title">
                {currentUser?.businessName || "Sunrise Artisan Bakery"}
              </h1>
              <p className="dashboard-subtitle">
                Manage your surplus food listings, review charity requests, and track your zero-waste impact.
              </p>
            </div>

            <button
              onClick={() => setShowPostModal(true)}
              className="btn btn-primary btn-lg btn-glow"
            >
              <PlusCircle size={20} />
              <span>Post New Surplus Food</span>
            </button>
          </div>
        </div>
      </div>

      <div className="container dashboard-body">
        {/* Role Notice */}
        {!isDonor && (
          <div className="role-alert-banner">
            <AlertCircle size={20} className="text-amber" />
            <div>
              <strong>Logged in as {currentUser.role}:</strong> Only registered Food Donors (Restaurants, Bakeries, Grocers) can post surplus food. You are currently viewing the Donor Portal preview.
            </div>
            <Link to="/browse-food" className="btn btn-sm btn-outline">
              Switch to Charity Food Browse
            </Link>
          </div>
        )}

        {/* Toast Alert */}
        {toastMsg && (
          <div className="auth-success-banner animate-fade-in-up">
            <CheckCircle2 size={20} className="text-emerald" />
            <span>{toastMsg}</span>
          </div>
        )}

        {/* Dashboard Metric Cards */}
        <div className="donor-stats-grid">
          <div className="d-stat-card">
            <div className="d-stat-icon bg-emerald-light">
              <Package size={22} className="text-emerald" />
            </div>
            <div>
              <div className="d-stat-val">160 portions</div>
              <div className="d-stat-lbl">Total Food Rescued</div>
            </div>
          </div>

          <div className="d-stat-card">
            <div className="d-stat-icon bg-amber-light">
              <Clock size={22} className="text-amber" />
            </div>
            <div>
              <div className="d-stat-val">{donations.filter(d => d.status !== 'Completed').length} Active</div>
              <div className="d-stat-lbl">Live Surplus Listings</div>
            </div>
          </div>

          <div className="d-stat-card">
            <div className="d-stat-icon bg-emerald-light">
              <TrendingUp size={22} className="text-emerald" />
            </div>
            <div>
              <div className="d-stat-val">72 kg CO₂</div>
              <div className="d-stat-lbl">Carbon Diversion Offset</div>
            </div>
          </div>
        </div>

        {/* Listings Section */}
        <div className="dashboard-section-box">
          <div className="box-header-flex">
            <div>
              <h3>Your Surplus Food Listings</h3>
              <p>Active listings are visible to verified charities for immediate claim.</p>
            </div>
            <button onClick={() => setShowPostModal(true)} className="btn btn-outline btn-sm">
              <PlusCircle size={14} />
              <span>Add Listing</span>
            </button>
          </div>

          <div className="listings-table-wrapper">
            <div className="listings-grid">
              {donations.map((item) => (
                <div key={item.id} className="listing-card">
                  <div className="listing-header">
                    <span className="badge badge-primary">{item.category}</span>
                    <span className={`status-pill status-${item.status.toLowerCase().replace(' ', '-')}`}>
                      {item.status}
                    </span>
                  </div>

                  <h4 className="listing-title">{item.foodTitle}</h4>
                  <p className="listing-notes">{item.notes}</p>

                  {/* Quantity Breakdown Bar */}
                  <div className="quantity-bar-group">
                    <div className="quantity-labels">
                      <span>Available Capacity:</span>
                      <strong>{item.remainingQuantity} of {item.totalQuantity} {item.unit}</strong>
                    </div>
                    <div className="progress-track">
                      <div 
                        className="progress-fill" 
                        style={{ width: `${(item.remainingQuantity / item.totalQuantity) * 100}%` }}
                      ></div>
                    </div>
                    <div className="partial-claim-note">
                      <CheckCircle2 size={12} className="text-emerald" />
                      <span>{item.claimedQuantity} {item.unit} claimed by community shelters</span>
                    </div>
                  </div>

                  <div className="listing-meta-footer">
                    <div className="meta-item">
                      <Clock size={14} className="text-amber" />
                      <span>{item.expiryTime}</span>
                    </div>
                    <div className="meta-item">
                      <Truck size={14} className="text-emerald" />
                      <span>{item.collectionMode}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Post Surplus Food Modal */}
      {showPostModal && (
        <div className="modal-backdrop" onClick={() => setShowPostModal(false)}>
          <div className="modal-container" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div>
                <div className="badge badge-primary">
                  <PlusCircle size={12} />
                  <span>Surplus Listing</span>
                </div>
                <h3 className="modal-title">Post Surplus Food for Rescue</h3>
                <p className="modal-subtitle">Specify portion quantities, expiry window, and handover mode</p>
              </div>
              <button className="modal-close-btn" onClick={() => setShowPostModal(false)}>✕</button>
            </div>

            <form onSubmit={handleCreateDonation} className="modal-form">
              <div className="form-group">
                <label className="form-label">Food Item Title *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="e.g. Sourdough Loaves & Pastry Assortment"
                  value={newPost.foodTitle}
                  onChange={(e) => setNewPost({ ...newPost, foodTitle: e.target.value })}
                  required
                />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Food Category *</label>
                  <select
                    className="form-input"
                    value={newPost.category}
                    onChange={(e) => setNewPost({ ...newPost, category: e.target.value })}
                  >
                    <option value="Cooked Meals">Cooked & Prepared Meals</option>
                    <option value="Bakery">Artisan Bakery & Pastries</option>
                    <option value="Fresh Produce">Fresh Fruits & Veggies</option>
                    <option value="Dairy & Chilled">Dairy & Chilled</option>
                    <option value="Packaged Dry">Packaged & Pantry Dry</option>
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Quantity (Portions / Meals) *</label>
                  <input
                    type="number"
                    min="1"
                    className="form-input"
                    value={newPost.quantity}
                    onChange={(e) => setNewPost({ ...newPost, quantity: e.target.value })}
                    required
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Availability Window (Hours before expiry) *</label>
                  <select
                    className="form-input"
                    value={newPost.hoursAvailable}
                    onChange={(e) => setNewPost({ ...newPost, hoursAvailable: Number(e.target.value) })}
                  >
                    <option value={2}>2 Hours (Immediate pickup)</option>
                    <option value={4}>4 Hours (Standard evening)</option>
                    <option value={8}>8 Hours (Full shift)</option>
                    <option value={24}>24 Hours (Next morning)</option>
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Handover Mode *</label>
                  <select
                    className="form-input"
                    value={newPost.collectionMode}
                    onChange={(e) => setNewPost({ ...newPost, collectionMode: e.target.value })}
                  >
                    <option value="Organization Pickup">Charity Organization Pickup</option>
                    <option value="Donor Delivery">Donor Direct Delivery</option>
                  </select>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Special Packaging / Allergen Notes</label>
                <textarea
                  rows={3}
                  className="form-input form-textarea"
                  placeholder="e.g. Vegetarian, packed in sealed trays, requires chilled transport."
                  value={newPost.notes}
                  onChange={(e) => setNewPost({ ...newPost, notes: e.target.value })}
                ></textarea>
              </div>

              <button type="submit" className="btn btn-primary btn-lg full-width">
                <span>Publish Surplus Listing</span>
                <ArrowRight size={18} />
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
