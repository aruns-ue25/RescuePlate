import React, { useState } from 'react';
import { 
  X, 
  User, 
  Lock, 
  Mail, 
  Building2, 
  MapPin, 
  Phone, 
  ShieldCheck, 
  Sparkles, 
  HeartHandshake, 
  Store, 
  Shield, 
  CheckCircle,
  Eye,
  EyeOff,
  AlertCircle
} from 'lucide-react';

export default function AuthModal({ isOpen, onClose, initialMode = 'login', initialRole = 'DONOR', onAuthSuccess, onShowToast }) {
  if (!isOpen) return null;

  const [mode, setMode] = useState(initialMode); // 'login' or 'register'
  const [role, setRole] = useState(initialRole); // 'DONOR', 'ORGANIZATION', 'ADMIN'
  const [showPassword, setShowPassword] = useState(false);

  // Form State
  const [formData, setFormData] = useState({
    name: '',
    businessName: '',
    email: '',
    phone: '',
    location: '',
    password: '',
    confirmPassword: '',
    donorType: 'Restaurant',
    acceptedFoodTypes: ['Cooked Meals', 'Bakery'],
    agreedToTerms: false
  });

  const [errorMsg, setErrorMsg] = useState('');
  const [isLoading, setIsLoading] = useState(false);

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

  // Demo autofill for quick testing
  const handleAutofillDemo = (demoRole) => {
    setRole(demoRole);
    if (demoRole === 'DONOR') {
      setFormData((prev) => ({
        ...prev,
        email: 'donor@sunrisebakery.com',
        password: 'Password@123',
        businessName: 'Sunrise Artisan Bakery',
        name: 'Elena Rostova',
        donorType: 'Bakery',
        location: '124 Central Ave, Metro City',
        phone: '+1 555-019-2834'
      }));
    } else if (demoRole === 'ORGANIZATION') {
      setFormData((prev) => ({
        ...prev,
        email: 'director@hopecommunity.org',
        password: 'Password@123',
        businessName: 'Hope Community Shelter',
        name: 'Sarah Jenkins',
        location: '78 Hope St, District 4',
        phone: '+1 555-014-9982'
      }));
    } else {
      setFormData((prev) => ({
        ...prev,
        email: 'admin@rescueplate.org',
        password: 'AdminSecurePass!99',
        businessName: 'RescuePlate Platform Admin',
        name: 'System Administrator',
        location: 'RescuePlate HQ',
        phone: '+1 800-555-0100'
      }));
    }
    setErrorMsg('');
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    setErrorMsg('');

    if (mode === 'register') {
      if (!formData.email || !formData.password || !formData.businessName) {
        setErrorMsg('Please fill in all required registration fields.');
        return;
      }
      if (formData.password.length < 6) {
        setErrorMsg('Password must be at least 6 characters long.');
        return;
      }
      if (formData.password !== formData.confirmPassword) {
        setErrorMsg('Passwords do not match. Please verify.');
        return;
      }
      if (!formData.agreedToTerms) {
        setErrorMsg('You must agree to the Food Safety & Platform Terms.');
        return;
      }
    } else {
      if (!formData.email || !formData.password) {
        setErrorMsg('Please provide your email and password.');
        return;
      }
    }

    setIsLoading(true);

    setTimeout(() => {
      setIsLoading(false);
      const userSession = {
        email: formData.email,
        name: formData.name || (role === 'DONOR' ? 'Chef Marco' : 'Charity Lead'),
        businessName: formData.businessName || (role === 'DONOR' ? 'Grand Bistro' : 'Hope Shelter'),
        role: role,
        token: 'mock-jwt-token-' + Math.random().toString(36).substring(2)
      };

      if (onAuthSuccess) {
        onAuthSuccess(userSession);
      }
      if (onShowToast) {
        onShowToast(
          mode === 'register'
            ? `Welcome to RescuePlate! Account registered as ${role}.`
            : `Signed in successfully as ${role}!`,
          'success'
        );
      }
      onClose();
    }, 900);
  };

  return (
    <div className="modal-backdrop animate-fade-in-up" onClick={onClose}>
      <div className="modal-container" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="modal-header">
          <div className="modal-brand-group">
            <div className="badge badge-primary">
              <Sparkles size={12} />
              <span>Secure Authentication</span>
            </div>
            <h3 className="modal-title">
              {mode === 'login' ? 'Sign In to RescuePlate' : 'Create an Account'}
            </h3>
            <p className="modal-subtitle">
              {mode === 'login'
                ? 'Select your user role and sign in with your credentials'
                : 'Join our redistribution network as a Food Donor or Charity'}
            </p>
          </div>
          <button className="modal-close-btn" onClick={onClose} aria-label="Close modal">
            <X size={20} />
          </button>
        </div>

        {/* Tab Switcher (Login vs Register) */}
        <div className="auth-tab-bar">
          <button
            type="button"
            className={`auth-tab-btn ${mode === 'login' ? 'active' : ''}`}
            onClick={() => {
              setMode('login');
              setErrorMsg('');
            }}
          >
            Sign In
          </button>
          <button
            type="button"
            className={`auth-tab-btn ${mode === 'register' ? 'active' : ''}`}
            onClick={() => {
              setMode('register');
              setErrorMsg('');
            }}
          >
            Register Account
          </button>
        </div>

        {/* Role Selector Grid */}
        <div className="role-select-section">
          <label className="input-label">Select Account Role</label>
          <div className="role-selector-cards">
            <button
              type="button"
              className={`role-card ${role === 'DONOR' ? 'role-card-active donor-theme' : ''}`}
              onClick={() => setRole('DONOR')}
            >
              <Store size={22} className="role-card-icon text-emerald" />
              <div className="role-card-name">Food Donor</div>
              <div className="role-card-hint">Hotels, Restaurants, Bakeries</div>
            </button>

            <button
              type="button"
              className={`role-card ${role === 'ORGANIZATION' ? 'role-card-active org-theme' : ''}`}
              onClick={() => setRole('ORGANIZATION')}
            >
              <HeartHandshake size={22} className="role-card-icon text-amber" />
              <div className="role-card-name">Organization</div>
              <div className="role-card-hint">Charities, Shelters, Pantries</div>
            </button>

            {mode === 'login' && (
              <button
                type="button"
                className={`role-card ${role === 'ADMIN' ? 'role-card-active admin-theme' : ''}`}
                onClick={() => setRole('ADMIN')}
              >
                <Shield size={22} className="role-card-icon text-accent" />
                <div className="role-card-name">Administrator</div>
                <div className="role-card-hint">System & User Oversight</div>
              </button>
            )}
          </div>
        </div>

        {/* Quick Autofill Helper for Fast Testing */}
        <div className="demo-fill-bar">
          <span className="demo-lbl">⚡ Quick Test:</span>
          <button
            type="button"
            className="demo-pill"
            onClick={() => handleAutofillDemo('DONOR')}
          >
            Fill Donor Demo
          </button>
          <button
            type="button"
            className="demo-pill"
            onClick={() => handleAutofillDemo('ORGANIZATION')}
          >
            Fill Charity Demo
          </button>
          {mode === 'login' && (
            <button
              type="button"
              className="demo-pill"
              onClick={() => handleAutofillDemo('ADMIN')}
            >
              Fill Admin Demo
            </button>
          )}
        </div>

        {/* Error Feedback Message */}
        {errorMsg && (
          <div className="auth-error-banner animate-fade-in-up">
            <AlertCircle size={16} />
            <span>{errorMsg}</span>
          </div>
        )}

        {/* Auth Form */}
        <form onSubmit={handleSubmit} className="modal-form">
          {mode === 'register' && (
            <>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">
                    {role === 'DONOR' ? 'Business / Restaurant Name *' : 'Organization Name *'}
                  </label>
                  <div className="input-with-icon">
                    <Building2 size={16} className="input-icon" />
                    <input
                      type="text"
                      className="form-input"
                      placeholder={role === 'DONOR' ? 'e.g. Sunrise Bakery' : 'e.g. Hope Community Shelter'}
                      value={formData.businessName}
                      onChange={(e) => handleInputChange('businessName', e.target.value)}
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
                      value={formData.name}
                      onChange={(e) => handleInputChange('name', e.target.value)}
                      required
                    />
                  </div>
                </div>
              </div>

              {role === 'DONOR' && (
                <div className="form-group">
                  <label className="form-label">Donor Business Sector</label>
                  <select
                    className="form-input"
                    value={formData.donorType}
                    onChange={(e) => handleInputChange('donorType', e.target.value)}
                  >
                    <option value="Restaurant">Restaurant / Bistro</option>
                    <option value="Bakery">Artisan Bakery & Pastry Shop</option>
                    <option value="Hotel">Hotel & Banquet Catering</option>
                    <option value="Supermarket">Supermarket / Grocery Retail</option>
                    <option value="Wholesale">Wholesale Food Distributor</option>
                  </select>
                </div>
              )}

              {role === 'ORGANIZATION' && (
                <div className="form-group">
                  <label className="form-label">Accepted Food Types (Select all that apply)</label>
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
                      placeholder="e.g. 124 Central Ave"
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
                      placeholder="+1 (555) 000-0000"
                      value={formData.phone}
                      onChange={(e) => handleInputChange('phone', e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </>
          )}

          {/* Email field */}
          <div className="form-group">
            <label className="form-label">Email Address *</label>
            <div className="input-with-icon">
              <Mail size={16} className="input-icon" />
              <input
                type="email"
                className="form-input"
                placeholder="name@organization.com"
                value={formData.email}
                onChange={(e) => handleInputChange('email', e.target.value)}
                required
              />
            </div>
            {mode === 'register' && (
              <span className="input-help-text">Must be a valid business or organization email address.</span>
            )}
          </div>

          {/* Password field */}
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

            {mode === 'register' && (
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
            )}
          </div>

          {mode === 'register' && (
            <div className="terms-checkbox-row">
              <input
                type="checkbox"
                id="terms"
                checked={formData.agreedToTerms}
                onChange={(e) => handleInputChange('agreedToTerms', e.target.checked)}
                className="custom-checkbox"
                required
              />
              <label htmlFor="terms" className="terms-label">
                I agree to the <a href="#about" className="link-text">RescuePlate Terms</a> and certify compliance with safe food handling standards.
              </label>
            </div>
          )}

          {/* Submit Button */}
          <button
            type="submit"
            disabled={isLoading}
            className={`btn ${role === 'DONOR' ? 'btn-primary' : role === 'ORGANIZATION' ? 'btn-amber' : 'btn-accent'} btn-lg full-width`}
          >
            {isLoading ? (
              <span>Authenticating...</span>
            ) : mode === 'login' ? (
              <span>Sign In as {role}</span>
            ) : (
              <span>Create {role} Account</span>
            )}
          </button>
        </form>

        {/* Modal Footer Switch */}
        <div className="modal-footer-switch">
          {mode === 'login' ? (
            <p>
              Don't have an account yet?{' '}
              <button
                type="button"
                className="link-btn"
                onClick={() => {
                  setMode('register');
                  setErrorMsg('');
                }}
              >
                Register here
              </button>
            </p>
          ) : (
            <p>
              Already registered?{' '}
              <button
                type="button"
                className="link-btn"
                onClick={() => {
                  setMode('login');
                  setErrorMsg('');
                }}
              >
                Sign in to your account
              </button>
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
