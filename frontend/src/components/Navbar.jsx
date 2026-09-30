import React, { useState, useEffect } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getProfileImageUrl, notificationApi } from '../services/api';
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
  Search,
  Bell,
  CheckCheck,
  CheckCircle2
} from 'lucide-react';

export default function Navbar() {
  const { currentUser, logout } = useAuth();
  const navigate = useNavigate();

  const [isScrolled, setIsScrolled] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  // Notification States
  const [notifications, setNotifications] = useState([]);
  const [notifOpen, setNotifOpen] = useState(false);
  const [notifLoading, setNotifLoading] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 20);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  // Fetch notifications for authenticated user
  const fetchNotifications = async () => {
    if (!currentUser) return;
    try {
      setNotifLoading(true);
      const res = await notificationApi.getMyNotifications();
      if (res && res.data) {
        setNotifications(res.data);
      }
    } catch (e) {
      // Quiet fail if not logged in or backend unavailable
    } finally {
      setNotifLoading(false);
    }
  };

  useEffect(() => {
    if (currentUser) {
      fetchNotifications();
      const interval = setInterval(fetchNotifications, 15000); // refresh every 15s
      return () => clearInterval(interval);
    } else {
      setNotifications([]);
    }
  }, [currentUser]);

  const handleMarkAsRead = async (id, e) => {
    e.stopPropagation();
    try {
      await notificationApi.markAsRead(id);
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
    } catch (err) {}
  };

  const handleMarkAllAsRead = async () => {
    try {
      await notificationApi.markAllAsRead();
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (err) {}
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;

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
          {currentUser?.role !== 'DONOR' && (
            <NavLink to="/browse-food" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
              Browse Food
            </NavLink>
          )}
          <NavLink to="/donors" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            Donors
          </NavLink>
          <NavLink to="/organizations" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
            Charities
          </NavLink>
          {currentUser?.role !== 'ORGANIZATION' && (
            <NavLink to="/donor-portal" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
              Post Surplus
            </NavLink>
          )}
          {currentUser && (
            <>
              <NavLink to="/requests" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
                Requests
              </NavLink>
              <NavLink to="/food-needs" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
                Food Needs
              </NavLink>
            </>
          )}
          {!currentUser && (
            <>
              <NavLink to="/how-it-works" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
                How It Works
              </NavLink>
              <NavLink to="/about" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
                About Us
              </NavLink>
              <NavLink to="/contact" className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}>
                Contact
              </NavLink>
            </>
          )}
        </nav>

        {/* Desktop Auth CTAs / Logged-in State */}
        <div className="nav-actions">
          {currentUser ? (
            <div className="logged-user-strip">
              {/* Notification Bell Button & Popover */}
              <div style={{ position: 'relative' }}>
                <button
                  onClick={() => {
                    setNotifOpen(!notifOpen);
                    if (!notifOpen) fetchNotifications();
                  }}
                  className="btn btn-ghost btn-sm"
                  style={{
                    position: 'relative',
                    padding: '8px',
                    borderRadius: '50%',
                    color: notifOpen ? '#047857' : '#4b5563',
                    background: notifOpen ? '#ecfdf5' : 'transparent',
                    cursor: 'pointer'
                  }}
                  title="Notifications"
                >
                  <Bell size={18} />
                  {unreadCount > 0 && (
                    <span 
                      style={{
                        position: 'absolute',
                        top: '2px',
                        right: '2px',
                        background: '#ef4444',
                        color: '#fff',
                        fontSize: '0.65rem',
                        fontWeight: 800,
                        width: '18px',
                        height: '18px',
                        borderRadius: '50%',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        border: '2px solid #fff'
                      }}
                    >
                      {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                  )}
                </button>

                {/* Notification Dropdown Popover */}
                {notifOpen && (
                  <div
                    style={{
                      position: 'absolute',
                      right: 0,
                      top: 'calc(100% + 10px)',
                      width: '340px',
                      background: '#fff',
                      borderRadius: '14px',
                      border: '1px solid #e5e7eb',
                      boxShadow: '0 10px 25px -5px rgba(0,0,0,0.15)',
                      zIndex: 10000,
                      overflow: 'hidden'
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '14px 16px', borderBottom: '1px solid #f3f4f6', background: '#f9fafb' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <Bell size={16} color="#047857" />
                        <strong style={{ fontSize: '0.95rem', color: '#111827' }}>Notifications</strong>
                        {unreadCount > 0 && (
                          <span style={{ fontSize: '0.72rem', background: '#fef2f2', color: '#dc2626', fontWeight: 700, padding: '2px 8px', borderRadius: '12px' }}>
                            {unreadCount} new
                          </span>
                        )}
                      </div>
                      {unreadCount > 0 && (
                        <button
                          onClick={handleMarkAllAsRead}
                          style={{ background: 'none', border: 'none', color: '#059669', fontSize: '0.78rem', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '4px' }}
                        >
                          <CheckCheck size={14} />
                          <span>Mark all read</span>
                        </button>
                      )}
                    </div>

                    <div style={{ maxHeight: '360px', overflowY: 'auto' }}>
                      {notifications.length === 0 ? (
                        <div style={{ padding: '32px 16px', textAlign: 'center', color: '#9ca3af', fontSize: '0.85rem' }}>
                          <Bell size={28} style={{ margin: '0 auto 8px', display: 'block', opacity: 0.4 }} />
                          No notifications yet.
                        </div>
                      ) : (
                        notifications.map(n => (
                          <div
                            key={n.id}
                            onClick={() => {
                              if (!n.isRead) handleMarkAsRead(n.id, { stopPropagation: () => {} });
                              setNotifOpen(false);
                              if (n.type.includes('OFFER')) {
                                navigate('/food-needs');
                              } else {
                                navigate('/requests');
                              }
                            }}
                            style={{
                              padding: '12px 16px',
                              borderBottom: '1px solid #f3f4f6',
                              background: n.isRead ? '#fff' : '#f0fdf4',
                              cursor: 'pointer',
                              transition: 'background 0.15s ease',
                              display: 'flex',
                              justifyContent: 'space-between',
                              alignItems: 'flex-start',
                              gap: '10px'
                            }}
                          >
                            <div style={{ flex: 1 }}>
                              <div style={{ fontSize: '0.85rem', fontWeight: 700, color: n.isRead ? '#374151' : '#065f46', marginBottom: '2px' }}>
                                {n.title}
                              </div>
                              <div style={{ fontSize: '0.8rem', color: '#4b5563', lineHeight: 1.4 }}>
                                {n.message}
                              </div>
                              <div style={{ fontSize: '0.72rem', color: '#9ca3af', marginTop: '4px' }}>
                                {new Date(n.createdAt).toLocaleDateString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                              </div>
                            </div>
                            {!n.isRead && (
                              <button
                                onClick={(e) => handleMarkAsRead(n.id, e)}
                                style={{ background: 'none', border: 'none', color: '#10b981', cursor: 'pointer', padding: '2px' }}
                                title="Mark as read"
                              >
                                <CheckCircle2 size={14} />
                              </button>
                            )}
                          </div>
                        ))
                      )}
                    </div>
                  </div>
                )}
              </div>

              <Link to="/profile" className="user-profile-badge" title="View Profile">
                {currentUser.profilePictureUrl ? (
                  <img
                    src={getProfileImageUrl(currentUser.profilePictureUrl)}
                    alt="Avatar"
                    className="nav-avatar-img"
                  />
                ) : (
                  <span className="user-role-dot"></span>
                )}
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
            {currentUser?.role !== 'DONOR' && (
              <Link to="/browse-food" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
                <span>Browse Surplus Food</span>
                <ChevronRight size={16} />
              </Link>
            )}
            <Link to="/donors" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>Browse Donors</span>
              <ChevronRight size={16} />
            </Link>
            <Link to="/organizations" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
              <span>Browse Charities</span>
              <ChevronRight size={16} />
            </Link>
            {currentUser?.role !== 'ORGANIZATION' && (
              <Link to="/donor-portal" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
                <span>Donor Portal / Post Food</span>
                <ChevronRight size={16} />
              </Link>
            )}
            {currentUser && (
              <>
                <Link to="/requests" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
                  <span>Food Requests</span>
                  <ChevronRight size={16} />
                </Link>
                <Link to="/food-needs" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
                  <span>Charity Food Needs</span>
                  <ChevronRight size={16} />
                </Link>
              </>
            )}
            {!currentUser && (
              <>
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
              </>
            )}
            {currentUser && (
              <Link to="/profile" className="mobile-nav-link" onClick={() => setMobileMenuOpen(false)}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  {currentUser.profilePictureUrl && (
                    <img
                      src={getProfileImageUrl(currentUser.profilePictureUrl)}
                      alt="Avatar"
                      className="nav-avatar-img"
                    />
                  )}
                  <span>My Profile</span>
                </div>
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
