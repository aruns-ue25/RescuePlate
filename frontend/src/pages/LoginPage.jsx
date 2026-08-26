import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  Lock, 
  Mail, 
  AlertCircle, 
  Eye, 
  EyeOff, 
  Sparkles,
  ArrowRight,
  ShieldCheck
} from 'lucide-react';

export default function LoginPage() {
  const navigate = useNavigate();
  const { login, loading } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');

    if (!email || !password) {
      setErrorMsg('Please enter both your email address and password.');
      return;
    }

    try {
      const res = await login(email.trim(), password);
      // Automatic smart redirection based on real database role
      if (res.user.role === 'DONOR') {
        navigate('/donor-portal');
      } else if (res.user.role === 'ORGANIZATION') {
        navigate('/browse-food');
      } else {
        navigate('/profile');
      }
    } catch (err) {
      setErrorMsg(err.message || 'Invalid email or password. Please try again.');
    }
  };

  return (
    <div className="auth-page-wrapper animate-fade-in-up">
      <div className="container auth-container">
        <div className="auth-card">
          {/* Header */}
          <div className="auth-header text-center">
            <div className="badge badge-primary auth-pill">
              <Sparkles size={14} />
              <span>RescuePlate Secure Login</span>
            </div>
            <h2 className="auth-title">Sign In to Your Account</h2>
            <p className="auth-subtitle">
              Enter your credentials to access your Donor, Charity, or Admin portal
            </p>
          </div>

          {/* Error Message */}
          {errorMsg && (
            <div className="auth-error-banner animate-fade-in-up">
              <AlertCircle size={16} />
              <span>{errorMsg}</span>
            </div>
          )}

          {/* Universal Login Form */}
          <form onSubmit={handleSubmit} className="modal-form">
            <div className="form-group">
              <label className="form-label">Email Address *</label>
              <div className="input-with-icon">
                <Mail size={16} className="input-icon" />
                <input
                  type="email"
                  className="form-input"
                  placeholder="e.g. name@business.com or charity@org.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Password *</label>
              <div className="input-with-icon">
                <Lock size={16} className="input-icon" />
                <input
                  type={showPassword ? 'text' : 'password'}
                  className="form-input"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
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

            <button
              type="submit"
              disabled={loading}
              className="btn btn-primary btn-lg full-width"
            >
              {loading ? (
                <span>Signing In...</span>
              ) : (
                <>
                  <span>Sign In</span>
                  <ArrowRight size={18} />
                </>
              )}
            </button>
          </form>

          {/* Security Notice */}
          <div className="login-security-footer">
            <ShieldCheck size={14} className="text-emerald" />
            <span>Secure 256-bit Encrypted Session • Role auto-detected</span>
          </div>

          {/* Footer Switch */}
          <div className="modal-footer-switch text-center">
            <p>
              Don't have an account yet?{' '}
              <Link to="/register" className="link-btn">
                Register here (Donor / Charity)
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
