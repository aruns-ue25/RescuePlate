import React, { useState, useEffect } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  Utensils, 
  Menu, 
  X, 
  ChevronRight, 
  User, 
  Sparkles, 
  LogOut, 
  Store, 
  HeartHandshake, 
  Search 
} from 'lucide-react';

export default function Navbar() {
  const { currentUser, logout } = useAuth();
  const navigate = useNavigate();

  const [isScrolled, setIsScrolled] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 20);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const handleSignOut = () => {
    logout();
    navigate('/');
    setMobileMenuOpen(false);
  };

  return (
    <header className={`navbar-header ${isScrolled ? 'navbar-scrolled' : ''}`}>
      <div className="container nav-container">
        {/* Brand Logo */}
        <Link to="/" className="brand-logo" onClick={() => setMobileMenuOpen(false)}>
          <div className="logo-icon-wrapper">
            <Utensils className="logo-icon" size={22} />
            <div className="logo-pulse"></div>
          </div>
          <div className="brand-text-group">
            <span className="brand-title">Rescue<span className="brand-accent">Plate</span></span>
            <span className="brand-tagline">Zero Food Waste</span>
          </div>
        </Link>

        {/* Desktop Nav Links */}
        <nav className="desktop-nav">
          <NavLink to="/" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            Home
          </NavLink>
          <NavLink to="/browse-food" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            Browse Food
          </NavLink>
          <NavLink to="/donor-portal" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            Post Surplus
          </NavLink>
          <NavLink to="/how-it-works" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            How It Works
          </NavLink>
          <NavLink to="/about" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            About Us
          </NavLink>
          <NavLink to="/contact" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            Contact
          </NavLink>
        </nav>

        {/* Desktop Auth CTAs / Logged-in State */}
        <div className="nav-actions">
          {currentUser ? (
            <div className="logged-user-strip">
              <Link to="/profile" className="user-profile-badge" title="View Profile">
                <span className="user-role-dot"></span>
                <span className="user-business-name">{currentUser.businessName || currentUser.name}</span>
                <span className="user-role-tag">{currentUser.role}</span>
              </Link>
              <button onClick={handleSignOut} className="btn btn-ghost btn-sm" title="Sign Out">
                <LogOut size={16} />
              </button>
            </div>
          ) : (
            <>
              <Link to="/login" className="btn btn-ghost">
                <User size={16} />
                <span>Sign In</span>
              </Link>
              <Link to="/register" className="btn btn-primary btn-glow">
                <Sparkles size={16} />
                <span>Join Platform</span>
              </Link>
            </>
          )}
        </div>

        {/* Mobile Hamburger Toggle */}
        <button
          className="mobile-toggle"
          onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
          aria-label="Toggle navigation menu"
        >
          {mobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
        </button>
      </div>

      {/* Mobile Drawer Menu */}
      {mobileMenuOpen && (
        <div className="mobile-drawer animate-fade-in-up">
          <nav className="mobile-nav-links">
            <Link to="/" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>Home</span>
              <ChevronRight size={16} />
            </Link>
            <Link to="/browse-food" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>Browse Surplus Food</span>
              <ChevronRight size={16} />
            </Link>
            <Link to="/donor-portal" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>Donor Portal / Post Food</span>
              <ChevronRight size={16} />
            </Link>
            <Link to="/how-it-works" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>How It Works</span>
              <ChevronRight size={16} />
            </Link>
            <Link to="/about" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>About Us</span>
              <ChevronRight size={16} />
            </Link>
            <Link to="/contact" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>Contact Support</span>
              <ChevronRight size={16} />
            </Link>
            {currentUser && (
              <Link to="/profile" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
                <span>My Profile</span>
                <ChevronRight size={16} />
              </Link>
            )}
          </nav>

          <div className="mobile-drawer-actions">
            {currentUser ? (
              <button onClick={handleSignOut} className="btn btn-outline full-width">
                <LogOut size={16} />
                <span>Sign Out ({currentUser.name})</span>
              </button>
            ) : (
              <>
                <Link to="/login" className="btn btn-outline full-width" onClick={() => setMobileMenuOpen(false)}>
                  <User size={16} />
                  <span>Sign In</span>
                </Link>
                <Link to="/register" className="btn btn-primary full-width" onClick={() => setMobileMenuOpen(false)}>
                  <Sparkles size={18} />
                  <span>Register (Donor / Charity)</span>
                </Link>
              </>
            )}
          </div>
        </div>
      )}
    </header>
  );
}
