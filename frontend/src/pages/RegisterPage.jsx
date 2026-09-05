import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  Building2, 
  User, 
  Mail, 
  Lock, 
  MapPin, 
  Phone, 
  Store, 
  HeartHandshake, 
  CheckCircle, 
  AlertCircle, 
  Eye, 
  EyeOff, 
  Sparkles,
  ArrowRight
} from 'lucide-react';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { register, loading } = useAuth();

  const [role, setRole] = useState('DONOR');
  const [formData, setFormData] = useState({
    businessOrOrgName: '',
    contactName: '',
    email: '',
    phone: '',
    location: '',
    password: '',
    confirmPassword: '',
    donorType: 'Restaurant',
    acceptedFoodTypes: ['Cooked Meals', 'Bakery'],
    agreedToTerms: false
  });

  const [showPassword, setShowPassword] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleInputChange = (field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errorMsg) setErrorMsg('');
  };

  const handleFoodTypeToggle = (type) => {
    setFormData((prev) => {
      const exists = prev.acceptedFoodTypes.includes(type);
      return {
        ...prev,
        acceptedFoodTypes: exists
          ? prev.acceptedFoodTypes.filter((t) => t !== type)
          : [...prev.acceptedFoodTypes, type]
      };
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');

    if (!formData.businessOrOrgName || !formData.email || !formData.password) {
      setErrorMsg('Please fill in all required fields.');
      return;
    }
    if (formData.password.length < 6) {
      setErrorMsg('Password must be at least 6 characters long.');
      return;
    }
    if (formData.password !== formData.confirmPassword) {
      setErrorMsg('Passwords do not match.');
      return;
    }
    if (!formData.agreedToTerms) {
      setErrorMsg('You must agree to the Platform Terms & Food Safety standards.');
      return;
    }

    try {
      const res = await register({ ...formData, role });
      if (role === 'DONOR') {
        navigate('/donor-portal');
      } else {
        navigate('/browse-food');
      }
    } catch (err) {
      setErrorMsg(err.message || 'Registration failed. Please check your information.');
    }
  };

  return (
    <div className="auth-page-wrapper animate-fade-in-up">
      <div className="container auth-container">
        <div className="auth-card register-card">
          {/* Header */}
          <div className="auth-header text-center">
            <div className="badge badge-primary auth-pill">
              <Sparkles size={14} />
              <span>Join RescuePlate</span>
            </div>
            <h2 className="auth-title">Create Your Account</h2>
            <p className="auth-subtitle">Choose whether you are donating surplus food or requesting for a charity</p>
          </div>

          {/* Role Switcher */}
          <div className="role-selector-cards role-two-cols">
            <button
              type="button"
              className={`role-card ${role === 'DONOR' ? 'role-card-active donor-theme' : ''}`}
              onClick={() => setRole('DONOR')}
            >
              <Store size={26} className="role-card-icon text-emerald" />
              <div className="role-card-name">Food Business / Donor</div>
              <div className="role-card-hint">Restaurants, Bakeries, Supermarkets, Hotels</div>
            </button>

            <button
              type="button"
              className={`role-card ${role === 'ORGANIZATION' ? 'role-card-active org-theme' : ''}`}
              onClick={() => setRole('ORGANIZATION')}
            >
              <HeartHandshake size={26} className="role-card-icon text-amber" />
              <div className="role-card-name">Charity / Nonprofit</div>
              <div className="role-card-hint">Shelters, Soup Kitchens, Pantries</div>
            </button>
          </div>

          {/* Error Banner */}
          {errorMsg && (
            <div className="auth-error-banner animate-fade-in-up">
              <AlertCircle size={16} />
              <span>{errorMsg}</span>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="modal-form">
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">
                  {role === 'DONOR' ? 'Business / Restaurant Name *' : 'Charity / Organization Name *'}
                </label>
                <div className="input-with-icon">
                  <Building2 size={16} className="input-icon" />
                  <input
                    type="text"
                    className="form-input"
                    placeholder={role === 'DONOR' ? 'e.g. Sunrise Artisan Bakery' : 'e.g. Hope Community Shelter'}
                    value={formData.businessOrOrgName}
                    onChange={(e) => handleInputChange('businessOrOrgName', e.target.value)}
                    required
                  />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Contact Person Name *</label>
                <div className="input-with-icon">
                  <User size={16} className="input-icon" />
                  <input
                    type="text"
                    className="form-input"
                    placeholder="e.g. Elena Rostova"
                    value={formData.contactName}
                    onChange={(e) => handleInputChange('contactName', e.target.value)}
                    required
                  />
                </div>
              </div>
            </div>

            {role === 'DONOR' && (
              <div className="form-group">
                <label className="form-label">Business Sector</label>
                <select
                  className="form-input"
                  value={formData.donorType}
                  onChange={(e) => handleInputChange('donorType', e.target.value)}
                >
                  <option value="Restaurant">Restaurant / Bistro</option>
                  <option value="Bakery">Artisan Bakery & Pastry Shop</option>
                  <option value="Hotel">Hotel & Banquet Catering</option>
                  <option value="Supermarket">Supermarket & Grocery Retail</option>
                  <option value="Wholesale">Wholesale Food Distributor</option>
                </select>
              </div>
            )}

            {role === 'ORGANIZATION' && (
              <div className="form-group">
                <label className="form-label">Accepted Food Categories</label>
                <div className="tags-multi-select">
                  {['Cooked Meals', 'Bakery', 'Fresh Produce', 'Dairy & Chilled', 'Packaged Dry'].map((foodType) => (
                    <button
                      key={foodType}
                      type="button"
                      className={`tag-btn ${formData.acceptedFoodTypes.includes(foodType) ? 'tag-btn-selected' : ''}`}
                      onClick={() => handleFoodTypeToggle(foodType)}
                    >
                      {formData.acceptedFoodTypes.includes(foodType) && <CheckCircle size={12} />}
                      <span>{foodType}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Physical Address / City *</label>
                <div className="input-with-icon">
                  <MapPin size={16} className="input-icon" />
                  <input
                    type="text"
                    className="form-input"
                    placeholder="e.g. 124 Central Ave, Metro City"
                    value={formData.location}
                    onChange={(e) => handleInputChange('location', e.target.value)}
                    required
                  />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Phone Number</label>
                <div className="input-with-icon">
                  <Phone size={16} className="input-icon" />
                  <input
                    type="tel"
                    className="form-input"
                    placeholder="+1 (555) 019-2834"
                    value={formData.phone}
                    onChange={(e) => handleInputChange('phone', e.target.value)}
                  />
                </div>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Email Address *</label>
              <div className="input-with-icon">
                <Mail size={16} className="input-icon" />
                <input
                  type="email"
                  className="form-input"
                  placeholder="contact@business.com"
                  value={formData.email}
                  onChange={(e) => handleInputChange('email', e.target.value)}
                  required
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Password *</label>
                <div className="input-with-icon">
                  <Lock size={16} className="input-icon" />
                  <input
                    type={showPassword ? 'text' : 'password'}
                    className="form-input"
                    placeholder="••••••••"
                    value={formData.password}
                    onChange={(e) => handleInputChange('password', e.target.value)}
                    required
                  />
                  <button
                    type="button"
                    className="password-toggle-btn"
                    onClick={() => setShowPassword(!showPassword)}
                    aria-label="Toggle password visibility"
                  >
                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Confirm Password *</label>
                <div className="input-with-icon">
                  <Lock size={16} className="input-icon" />
                  <input
                    type={showPassword ? 'text' : 'password'}
                    className="form-input"
                    placeholder="••••••••"
                    value={formData.confirmPassword}
                    onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
                    required
                  />
                </div>
              </div>
            </div>

            <div className="terms-checkbox-row">
              <input
                type="checkbox"
                id="reg-terms"
                checked={formData.agreedToTerms}
                onChange={(e) => handleInputChange('agreedToTerms', e.target.checked)}
                className="custom-checkbox"
                required
              />
              <label htmlFor="reg-terms" className="terms-label">
                I agree to the <Link to="/about" className="link-text">RescuePlate Terms</Link> and certify adherence to safe food handling and redistribution protocols.
              </label>
            </div>

            <button
              type="submit"
              disabled={loading}
              className={`btn ${role === 'DONOR' ? 'btn-primary' : 'btn-amber'} btn-lg full-width`}
            >
              {loading ? (
                <span>Creating Account...</span>
              ) : (
                <>
                  <span>Create {role === 'DONOR' ? 'Donor' : 'Charity'} Account</span>
                  <ArrowRight size={18} />
                </>
              )}
            </button>
          </form>

          {/* Footer Switch */}
          <div className="modal-footer-switch text-center">
            <p>
              Already registered?{' '}
              <Link to="/login" className="link-btn">
                Sign in here
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
