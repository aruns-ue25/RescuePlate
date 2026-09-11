import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { 
  Search, 
  Filter, 
  MapPin, 
  Clock, 
  Truck, 
  HeartHandshake, 
  CheckCircle2, 
  AlertCircle, 
  Building2, 
  Sparkles, 
  ArrowRight, 
  Package, 
  Check 
} from 'lucide-react';

export default function OrganizationBrowsePage() {
  const { currentUser } = useAuth();

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('ALL');

  // Simulated live marketplace inventory of surplus food
  const [availableDonations, setAvailableDonations] = useState([
    {
      id: "DON-201",
      donorName: "Sunrise Artisan Bakery",
      donorType: "Bakery",
      foodTitle: "Fresh Sourdough Loaves & Croissants",
      category: "Bakery",
      emoji: "🥐",
      totalQuantity: 100,
      claimedQuantity: 35,
      remainingQuantity: 65,
      unit: "portions",
      distance: "1.2 km away",
      location: "124 Central Ave",
      expiryHours: 3.5,
      collectionMode: "Organization Pickup",
      dietary: ["Vegetarian", "Dairy-Free"],
      notes: "Baked fresh today at 4:00 PM. Packed in sanitized thermal boxes."
    },
    {
      id: "DON-202",
      donorName: "Grand Azure Hotel",
      donorType: "Hotel",
      foodTitle: "Banquet Hot Meals (Rice, Steamed Veggies & Chicken)",
      category: "Cooked Meals",
      emoji: "🍲",
      totalQuantity: 80,
      claimedQuantity: 20,
      remainingQuantity: 60,
      unit: "meal boxes",
      distance: "2.8 km away",
      location: "Grand Azure Banquet Hall",
      expiryHours: 2.0,
      collectionMode: "Donor Delivery",
      dietary: ["Halal Certified", "High Protein"],
      notes: "Surplus from private luncheon. Maintained at safe holding temperatures."
    },
    {
      id: "DON-203",
      donorName: "Green Harvest Organic Market",
      donorType: "Supermarket",
      foodTitle: "Fresh Farm Greens, Apples & Seasonal Veggies",
      category: "Fresh Produce",
      emoji: "🥗",
      totalQuantity: 150,
      claimedQuantity: 40,
      remainingQuantity: 110,
      unit: "kg",
      distance: "3.4 km away",
      location: "Green Harvest Supermarket, Bay St",
      expiryHours: 24.0,
      collectionMode: "Organization Pickup",
      dietary: ["100% Organic", "Vegan"],
      notes: "Baskets of fresh crisp produce ready for immediate pantry distribution."
    },
    {
      id: "DON-204",
      donorName: "Metro Dairy Mart",
      donorType: "Supermarket",
      foodTitle: "Pasteurized Milk & Greek Yogurts (Sealed)",
      category: "Dairy & Chilled",
      emoji: "🥛",
      totalQuantity: 50,
      claimedQuantity: 0,
      remainingQuantity: 50,
      unit: "chilled units",
      distance: "4.1 km away",
      location: "Metro Depot 3",
      expiryHours: 12.0,
      collectionMode: "Organization Pickup",
      dietary: ["Refrigerated"],
      notes: "Sealed cartons 3 days before best-before date. Cold storage required."
    }
  ]);

  // Claim Modal State
  const [selectedDonation, setSelectedDonation] = useState(null);
  const [requestedPortions, setRequestedPortions] = useState(25);
  const [confirmToast, setConfirmToast] = useState('');

  const handleOpenClaimModal = (item) => {
    if (!currentUser) {
      setConfirmToast('Please sign in as a Charity Organization to request surplus food.');
      setTimeout(() => setConfirmToast(''), 5000);
      return;
    }
    if (currentUser.role !== 'ORGANIZATION') {
      setConfirmToast('Only registered Charity Organizations can claim food donations. Food Donors cannot claim food.');
      setTimeout(() => setConfirmToast(''), 5000);
      return;
    }
    setSelectedDonation(item);
    setRequestedPortions(Math.min(25, item.remainingQuantity));
  };

  const handleConfirmClaim = (e) => {
    e.preventDefault();
    if (!selectedDonation) return;

    const qty = Number(requestedPortions);
    if (qty <= 0 || qty > selectedDonation.remainingQuantity) return;

    // Update inventory (SRS Partial Fulfilment)
    setAvailableDonations((prev) =>
      prev.map((d) => {
        if (d.id === selectedDonation.id) {
          const newClaimed = d.claimedQuantity + qty;
          const newRemaining = d.remainingQuantity - qty;
          return {
            ...d,
            claimedQuantity: newClaimed,
            remainingQuantity: newRemaining
          };
        }
        return d;
      })
    );

    setConfirmToast(
      `Success! You requested ${qty} ${selectedDonation.unit} of "${selectedDonation.foodTitle}". Donor notified for handover.`
    );
    setSelectedDonation(null);
    setTimeout(() => setConfirmToast(''), 6000);
  };

  // Filter listings
  const filteredDonations = availableDonations.filter((item) => {
    const matchesCat = selectedCategory === 'ALL' || item.category === selectedCategory;
    const matchesSearch =
      item.foodTitle.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.donorName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.category.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesCat && matchesSearch;
  });

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
            Discover safe, surplus meals from local restaurants and bakeries. Request the exact quantity your shelter needs.
          </p>

          {/* Search & Filter Bar */}
          <div className="marketplace-search-bar">
            <div className="search-input-box">
              <Search size={18} className="search-icon" />
              <input
                type="text"
                className="search-input"
                placeholder="Search food by name, category, or donor..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>

            <div className="category-filter-pills">
              {[
                { id: 'ALL', label: 'All Categories' },
                { id: 'Cooked Meals', label: '🍲 Cooked Meals' },
                { id: 'Bakery', label: '🥐 Bakery' },
                { id: 'Fresh Produce', label: '🥗 Produce' },
                { id: 'Dairy & Chilled', label: '🥛 Dairy' }
              ].map((cat) => (
                <button
                  key={cat.id}
                  onClick={() => setSelectedCategory(cat.id)}
                  className={`filter-pill-btn ${selectedCategory === cat.id ? 'active' : ''}`}
                >
                  {cat.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="container dashboard-body">
        {/* Success Alert Toast */}
        {confirmToast && (
          <div className="auth-success-banner animate-fade-in-up">
            <CheckCircle2 size={22} className="text-emerald" />
            <span>{confirmToast}</span>
          </div>
        )}

        {/* Results Counter */}
        <div className="results-counter-strip">
          <span>
            Showing <strong>{filteredDonations.length}</strong> available food donation listings
          </span>
          <span className="live-pulse-badge">
            <span className="pulse-dot"></span> Live Redistribution Feed
          </span>
        </div>

        {/* Food Products Grid */}
        <div className="food-catalog-grid">
          {filteredDonations.map((item) => (
            <div key={item.id} className="food-product-card">
              <div className="product-top-row">
                <div className="product-emoji-bubble">{item.emoji}</div>
                <div className="product-donor-info">
                  <span className="donor-name-tag">{item.donorName}</span>
                  <span className="distance-tag">
                    <MapPin size={12} /> {item.distance}
                  </span>
                </div>
              </div>

              <h3 className="product-title">{item.foodTitle}</h3>
              <p className="product-notes">{item.notes}</p>

              <div className="dietary-tags-row">
                {item.dietary.map((tag, i) => (
                  <span key={i} className="diet-tag">
                    {tag}
                  </span>
                ))}
              </div>

              {/* Partial Fulfilment Progress Bar */}
              <div className="quantity-bar-group">
                <div className="quantity-labels">
                  <span className="qty-title">Available for Claim</span>
                  <span className="qty-numbers">
                    <strong>{item.remainingQuantity}</strong> of {item.totalQuantity} {item.unit} left
                  </span>
                </div>
                <div className="progress-track">
                  <div
                    className="progress-fill"
                    style={{
                      width: `${(item.remainingQuantity / item.totalQuantity) * 100}%`
                    }}
                  ></div>
                </div>
              </div>

              {/* Card Footer Details */}
              <div className="product-card-footer">
                <div className="expiry-tag">
                  <Clock size={14} className="text-amber" />
                  <span>{item.expiryHours}h window remaining</span>
                </div>

                <button
                  onClick={() => handleOpenClaimModal(item)}
                  disabled={item.remainingQuantity <= 0}
                  className={`btn ${item.remainingQuantity > 0 ? 'btn-amber' : 'btn-ghost'} full-width`}
                >
                  {item.remainingQuantity > 0 ? (
                    <>
                      <HeartHandshake size={16} />
                      <span>Request Quantity (Claim)</span>
                    </>
                  ) : (
                    <span>Fully Claimed</span>
                  )}
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Claim / Request Modal (SRS Partial Fulfilment) */}
      {selectedDonation && (
        <div className="modal-backdrop" onClick={() => setSelectedDonation(null)}>
          <div className="modal-container" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div>
                <div className="badge badge-amber">
                  <HeartHandshake size={12} />
                  <span>Partial Fulfilment Request</span>
                </div>
                <h3 className="modal-title">Request Surplus Food</h3>
                <p className="modal-subtitle">{selectedDonation.foodTitle}</p>
              </div>
              <button className="modal-close-btn" onClick={() => setSelectedDonation(null)}>✕</button>
            </div>

            <form onSubmit={handleConfirmClaim} className="modal-form">
              <div className="claim-item-summary">
                <div className="summary-donor">
                  <Building2 size={16} className="text-emerald" />
                  <span>Provided by: <strong>{selectedDonation.donorName}</strong></span>
                </div>
                <div className="summary-avail">
                  <span>Available Balance: <strong>{selectedDonation.remainingQuantity} {selectedDonation.unit}</strong></span>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Quantity Needed by Your Organization *</label>
                <input
                  type="number"
                  min="1"
                  max={selectedDonation.remainingQuantity}
                  value={requestedPortions}
                  onChange={(e) => setRequestedPortions(Math.min(selectedDonation.remainingQuantity, Math.max(1, Number(e.target.value))))}
                  className="form-input"
                  required
                />
                <span className="input-help-text">
                  You can request any quantity up to {selectedDonation.remainingQuantity} {selectedDonation.unit}.
                </span>
              </div>

              <div className="form-group">
                <label className="form-label">Pickup & Collection Arrangement</label>
                <div className="info-box-pickup">
                  <Truck size={18} className="text-amber" />
                  <div>
                    <strong>{selectedDonation.collectionMode}</strong>
                    <p>Pickup address: {selectedDonation.location}</p>
                  </div>
                </div>
              </div>

              <div className="terms-checkbox-row">
                <input type="checkbox" id="claim-terms" defaultChecked required className="custom-checkbox" />
                <label htmlFor="claim-terms" className="terms-label">
                  We confirm this food will be stored and distributed safely to individuals in need.
                </label>
              </div>

              <button type="submit" className="btn btn-amber btn-lg full-width">
                <Check size={18} />
                <span>Confirm Request for {requestedPortions} {selectedDonation.unit}</span>
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
