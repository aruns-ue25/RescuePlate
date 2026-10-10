import React, { useState, useEffect } from 'react';
import { authApi } from '../services/api';
import { useAuth } from '../context/AuthContext';

export default function AdminDashboardPage() {
  const { currentUser } = useAuth();
  const [activeTab, setActiveTab] = useState('users'); // 'overview', 'users', 'donations', 'logs'

  // Data states
  const [users, setUsers] = useState([]);
  const [overviewData, setOverviewData] = useState(null);
  const [activityLogs, setActivityLogs] = useState([]);

  // Filter & Search states for User Management
  const [searchQuery, setSearchQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState('ALL');
  const [statusFilter, setStatusFilter] = useState('ALL');

  // Loading & Alert states
  const [loading, setLoading] = useState(true);
  const [actionLoadingId, setActionLoadingId] = useState(null);
  const [alert, setAlert] = useState({ type: '', message: '' });

  useEffect(() => {
    fetchAdminData();
  }, []);

  const fetchAdminData = async () => {
    setLoading(true);
    setAlert({ type: '', message: '' });
    try {
      // 1. Fetch Users
      const usersRes = await authApi.getAdminUsers();
      if (usersRes.success && usersRes.data) {
        setUsers(usersRes.data);
      }

      // 2. Fetch Monitoring Overview
      const overviewRes = await authApi.getAdminMonitoringOverview();
      if (overviewRes.success && overviewRes.data) {
        setOverviewData(overviewRes.data);
      }

      // 3. Fetch Activity Logs
      const logsRes = await authApi.getAdminActivityLogs(1);
      if (logsRes.success && logsRes.data) {
        setActivityLogs(logsRes.data);
      }
    } catch (err) {
      setAlert({
        type: 'danger',
        message: err.message || 'Failed to load administrator telemetry data. Make sure backend is running.'
      });
    } finally {
      setLoading(false);
    }
  };

  const handleToggleStatus = async (userId, currentStatus) => {
    const newStatus = !currentStatus;
    const actionText = newStatus ? 'reactivate' : 'deactivate';

    if (!window.confirm(`Are you sure you want to ${actionText} this user account?`)) {
      return;
    }

    setActionLoadingId(userId);
    setAlert({ type: '', message: '' });

    try {
      const res = await authApi.toggleUserStatus(userId, newStatus);
      if (res.success) {
        setAlert({
          type: 'success',
          message: res.message || `User account successfully ${newStatus ? 'activated' : 'deactivated'}.`
        });
        // Update local state
        setUsers((prev) =>
          prev.map((u) => (u.id === userId ? { ...u, isActive: newStatus } : u))
        );
        // Refresh logs
        const logsRes = await authApi.getAdminActivityLogs(1);
        if (logsRes.success && logsRes.data) {
          setActivityLogs(logsRes.data);
        }
      }
    } catch (err) {
      setAlert({
        type: 'danger',
        message: err.message || `Failed to ${actionText} user account.`
      });
    } finally {
      setActionLoadingId(null);
    }
  };

  // Filtered users calculation
  const filteredUsers = users.filter((user) => {
    const query = searchQuery.toLowerCase();
    const matchesQuery =
      user.email.toLowerCase().includes(query) ||
      (user.businessName && user.businessName.toLowerCase().includes(query)) ||
      (user.location && user.location.toLowerCase().includes(query));

    const matchesRole = roleFilter === 'ALL' || user.role === roleFilter;
    const matchesStatus =
      statusFilter === 'ALL' ||
      (statusFilter === 'ACTIVE' && user.isActive) ||
      (statusFilter === 'INACTIVE' && !user.isActive);

    return matchesQuery && matchesRole && matchesStatus;
  });

  const formatDate = (dateStr) => {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getActionBadgeColor = (action) => {
    if (action.includes('SUCCESS') || action.includes('REACTIVATED')) return '#10b981';
    if (action.includes('FAILED') || action.includes('DEACTIVATED')) return '#ef4444';
    if (action.includes('LOCKOUT')) return '#f59e0b';
    return '#3b82f6';
  };

  return (
    <div style={{
      backgroundColor: '#0f172a',
      color: '#f8fafc',
      minHeight: '90vh',
      padding: '2rem 1.5rem',
      fontFamily: 'system-ui, -apple-system, sans-serif'
    }}>
      <div style={{ maxWidth: '1280px', margin: '0 auto' }}>
        {/* Top Banner Header */}
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          backgroundColor: '#1e293b',
          padding: '1.5rem 2rem',
          borderRadius: '16px',
          border: '1px solid rgba(255,255,255,0.1)',
          marginBottom: '2rem',
          flexWrap: 'wrap',
          gap: '1rem'
        }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <span style={{
                backgroundColor: '#ef4444',
                color: '#ffffff',
                fontSize: '0.75rem',
                fontWeight: '800',
                padding: '0.2rem 0.6rem',
                borderRadius: '4px',
                letterSpacing: '1px'
              }}>
                VERIFIED ADMIN
              </span>
              <h1 style={{ fontSize: '1.8rem', fontWeight: '800', margin: 0 }}>
                System Administration Hub
              </h1>
            </div>
            <p style={{ color: '#94a3b8', fontSize: '0.9rem', marginTop: '0.4rem', margin: 0 }}>
              Logged in as <strong style={{ color: '#e2e8f0' }}>{currentUser?.email}</strong> (Role: ADMIN)
            </p>
          </div>

          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <button
              onClick={fetchAdminData}
              disabled={loading}
              style={{
                padding: '0.65rem 1.25rem',
                backgroundColor: '#334155',
                color: '#f8fafc',
                border: 'none',
                borderRadius: '8px',
                fontWeight: '600',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem'
              }}
            >
              🔄 {loading ? 'Refreshing...' : 'Refresh Telemetry'}
            </button>
          </div>
        </div>

        {/* Global Alert Notification */}
        {alert.message && (
          <div style={{
            backgroundColor: alert.type === 'success' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
            borderLeft: `4px solid ${alert.type === 'success' ? '#10b981' : '#ef4444'}`,
            padding: '1rem 1.25rem',
            borderRadius: '8px',
            color: alert.type === 'success' ? '#a7f3d0' : '#fca5a5',
            marginBottom: '1.5rem',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center'
          }}>
            <span>{alert.message}</span>
            <button
              onClick={() => setAlert({ type: '', message: '' })}
              style={{ background: 'none', border: 'none', color: 'inherit', cursor: 'pointer', fontWeight: '700' }}
            >
              ✕
            </button>
          </div>
        )}

        {/* Navigation Tabs */}
        <div style={{
          display: 'flex',
          gap: '0.5rem',
          borderBottom: '1px solid #334155',
          marginBottom: '2rem'
        }}>
          {[
            { id: 'overview', label: '📊 Platform Overview', count: null },
            { id: 'users', label: '👥 User Directory & Control', count: users.length },
            { id: 'logs', label: '📜 System Activity & Audit Trail', count: activityLogs.length }
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              style={{
                padding: '0.85rem 1.5rem',
                backgroundColor: activeTab === tab.id ? '#1e293b' : 'transparent',
                color: activeTab === tab.id ? '#38bdf8' : '#94a3b8',
                border: 'none',
                borderBottom: activeTab === tab.id ? '3px solid #38bdf8' : '3px solid transparent',
                fontWeight: '700',
                fontSize: '0.95rem',
                cursor: 'pointer',
                borderRadius: '8px 8px 0 0',
                transition: 'all 0.2s ease'
              }}
            >
              {tab.label} {tab.count !== null && <span style={{ opacity: 0.75 }}>({tab.count})</span>}
            </button>
          ))}
        </div>

        {/* TAB 1: EXECUTIVE OVERVIEW */}
        {activeTab === 'overview' && (
          <div>
            {/* KPI Cards Grid */}
            <div style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
              gap: '1.25rem',
              marginBottom: '2rem'
            }}>
              <div style={{ backgroundColor: '#1e293b', padding: '1.5rem', borderRadius: '12px', border: '1px solid #334155' }}>
                <span style={{ fontSize: '0.85rem', color: '#94a3b8', fontWeight: '600' }}>TOTAL REGISTERED USERS</span>
                <div style={{ fontSize: '2.25rem', fontWeight: '800', color: '#ffffff', marginTop: '0.25rem' }}>
                  {overviewData?.totalUsers ?? users.length}
                </div>
                <span style={{ fontSize: '0.75rem', color: '#10b981' }}>Across all user roles</span>
              </div>

              <div style={{ backgroundColor: '#1e293b', padding: '1.5rem', borderRadius: '12px', border: '1px solid #334155' }}>
                <span style={{ fontSize: '0.85rem', color: '#94a3b8', fontWeight: '600' }}>ACTIVE FOOD DONORS</span>
                <div style={{ fontSize: '2.25rem', fontWeight: '800', color: '#38bdf8', marginTop: '0.25rem' }}>
                  {overviewData?.activeDonors ?? users.filter((u) => u.role === 'DONOR' && u.isActive).length}
                </div>
                <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>Restaurants & Bakeries</span>
              </div>

              <div style={{ backgroundColor: '#1e293b', padding: '1.5rem', borderRadius: '12px', border: '1px solid #334155' }}>
                <span style={{ fontSize: '0.85rem', color: '#94a3b8', fontWeight: '600' }}>CHARITY ORGANIZATIONS</span>
                <div style={{ fontSize: '2.25rem', fontWeight: '800', color: '#a855f7', marginTop: '0.25rem' }}>
                  {overviewData?.activeOrganizations ?? users.filter((u) => u.role === 'ORGANIZATION' && u.isActive).length}
                </div>
                <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>Verified Non-Profits</span>
              </div>

              <div style={{ backgroundColor: '#1e293b', padding: '1.5rem', borderRadius: '12px', border: '1px solid #334155' }}>
                <span style={{ fontSize: '0.85rem', color: '#94a3b8', fontWeight: '600' }}>SYSTEM HEALTH STATUS</span>
                <div style={{ fontSize: '1.5rem', fontWeight: '700', color: '#10b981', marginTop: '0.5rem' }}>
                  ● ONLINE & SECURE
                </div>
                <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>VerifiedAdminOnly Active</span>
              </div>
            </div>

            {/* Microservices Status Card */}
            <div style={{
              backgroundColor: '#1e293b',
              padding: '1.75rem',
              borderRadius: '12px',
              border: '1px solid #334155',
              marginBottom: '2rem'
            }}>
              <h3 style={{ fontSize: '1.1rem', fontWeight: '700', marginBottom: '1rem', color: '#f8fafc' }}>
                Microservices Telemetry & Domain Boundaries
              </h3>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1rem' }}>
                <div style={{ backgroundColor: '#0f172a', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid #10b981' }}>
                  <div style={{ fontWeight: '700', color: '#ffffff' }}>UserService (Port 5000)</div>
                  <div style={{ fontSize: '0.8rem', color: '#94a3b8', marginTop: '0.2rem' }}>Status: Operational (Auth, Users, Audit)</div>
                </div>

                <div style={{ backgroundColor: '#0f172a', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid #38bdf8' }}>
                  <div style={{ fontWeight: '700', color: '#ffffff' }}>DonationService (Port 5001)</div>
                  <div style={{ fontSize: '0.8rem', color: '#94a3b8', marginTop: '0.2rem' }}>
                    Status: {overviewData?.donationSummary?.status === 'UNAVAILABLE' ? '⚠️ Offline / Unreachable' : 'Operational (Surplus Food Listings)'}
                  </div>
                </div>

                <div style={{ backgroundColor: '#0f172a', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid #a855f7' }}>
                  <div style={{ fontWeight: '700', color: '#ffffff' }}>RequestWorkflowService (Port 5002)</div>
                  <div style={{ fontSize: '0.8rem', color: '#94a3b8', marginTop: '0.2rem' }}>
                    Status: {overviewData?.requestSummary?.status === 'UNAVAILABLE' ? '⚠️ Offline / Unreachable' : 'Operational (Food Claims & Workflow)'}
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* TAB 2: USER DIRECTORY & CONTROL (STORY 3) */}
        {activeTab === 'users' && (
          <div>
            {/* Search and Filters bar */}
            <div style={{
              display: 'flex',
              gap: '1rem',
              marginBottom: '1.5rem',
              flexWrap: 'wrap',
              backgroundColor: '#1e293b',
              padding: '1.25rem',
              borderRadius: '12px',
              border: '1px solid #334155'
            }}>
              <div style={{ flex: '2', minWidth: '240px' }}>
                <input
                  type="text"
                  placeholder="🔍 Search users by email, business name, or location..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '0.75rem 1rem',
                    backgroundColor: '#0f172a',
                    border: '1px solid #334155',
                    borderRadius: '8px',
                    color: '#ffffff',
                    fontSize: '0.9rem',
                    outline: 'none',
                    boxSizing: 'border-box'
                  }}
                />
              </div>

              <div style={{ flex: '1', minWidth: '160px' }}>
                <select
                  value={roleFilter}
                  onChange={(e) => setRoleFilter(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '0.75rem 1rem',
                    backgroundColor: '#0f172a',
                    border: '1px solid #334155',
                    borderRadius: '8px',
                    color: '#ffffff',
                    fontSize: '0.9rem',
                    outline: 'none'
                  }}
                >
                  <option value="ALL">All Roles</option>
                  <option value="DONOR">DONOR</option>
                  <option value="ORGANIZATION">ORGANIZATION</option>
                  <option value="ADMIN">ADMIN</option>
                </select>
              </div>

              <div style={{ flex: '1', minWidth: '160px' }}>
                <select
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '0.75rem 1rem',
                    backgroundColor: '#0f172a',
                    border: '1px solid #334155',
                    borderRadius: '8px',
                    color: '#ffffff',
                    fontSize: '0.9rem',
                    outline: 'none'
                  }}
                >
                  <option value="ALL">All Statuses</option>
                  <option value="ACTIVE">Active Only</option>
                  <option value="INACTIVE">Inactive Only</option>
                </select>
              </div>
            </div>

            {/* Users Data Table */}
            <div style={{
              backgroundColor: '#1e293b',
              borderRadius: '12px',
              border: '1px solid #334155',
              overflow: 'hidden'
            }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ backgroundColor: '#0f172a', borderBottom: '1px solid #334155', color: '#94a3b8' }}>
                    <th style={{ padding: '1rem 1.25rem' }}>Account Email</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Role</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Business / Org Name</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Location</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Registered Date</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Status</th>
                    <th style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredUsers.length === 0 ? (
                    <tr>
                      <td colSpan="7" style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>
                        No registered users match the specified search and filter criteria.
                      </td>
                    </tr>
                  ) : (
                    filteredUsers.map((user) => (
                      <tr key={user.id} style={{ borderBottom: '1px solid #334155' }}>
                        <td style={{ padding: '1rem 1.25rem', fontWeight: '600', color: '#ffffff' }}>
                          {user.email}
                        </td>
                        <td style={{ padding: '1rem 1.25rem' }}>
                          <span style={{
                            padding: '0.25rem 0.6rem',
                            borderRadius: '4px',
                            fontSize: '0.75rem',
                            fontWeight: '700',
                            backgroundColor:
                              user.role === 'ADMIN' ? 'rgba(239, 68, 68, 0.2)' :
                              user.role === 'DONOR' ? 'rgba(56, 189, 248, 0.2)' : 'rgba(168, 85, 247, 0.2)',
                            color:
                              user.role === 'ADMIN' ? '#fca5a5' :
                              user.role === 'DONOR' ? '#7dd3fc' : '#c084fc'
                          }}>
                            {user.role}
                          </span>
                        </td>
                        <td style={{ padding: '1rem 1.25rem', color: '#cbd5e1' }}>
                          {user.businessName || 'System Admin'}
                        </td>
                        <td style={{ padding: '1rem 1.25rem', color: '#94a3b8' }}>
                          {user.location || 'N/A'}
                        </td>
                        <td style={{ padding: '1rem 1.25rem', color: '#94a3b8', fontSize: '0.85rem' }}>
                          {formatDate(user.createdAt)}
                        </td>
                        <td style={{ padding: '1rem 1.25rem' }}>
                          <span style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '0.35rem',
                            padding: '0.25rem 0.65rem',
                            borderRadius: '20px',
                            fontSize: '0.75rem',
                            fontWeight: '700',
                            backgroundColor: user.isActive ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)',
                            color: user.isActive ? '#6ee7b7' : '#fca5a5'
                          }}>
                            ● {user.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>
                          {user.role === 'ADMIN' ? (
                            <span style={{ fontSize: '0.8rem', color: '#64748b' }}>Protected Admin</span>
                          ) : (
                            <button
                              onClick={() => handleToggleStatus(user.id, user.isActive)}
                              disabled={actionLoadingId === user.id}
                              style={{
                                padding: '0.4rem 0.85rem',
                                backgroundColor: user.isActive ? '#dc2626' : '#059669',
                                color: '#ffffff',
                                border: 'none',
                                borderRadius: '6px',
                                fontSize: '0.8rem',
                                fontWeight: '600',
                                cursor: actionLoadingId === user.id ? 'not-allowed' : 'pointer',
                                transition: 'all 0.15s ease'
                              }}
                            >
                              {actionLoadingId === user.id
                                ? 'Updating...'
                                : user.isActive
                                ? 'Deactivate'
                                : 'Reactivate'}
                            </button>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* TAB 3: AUDIT ACTIVITY LOGS */}
        {activeTab === 'logs' && (
          <div>
            <div style={{
              backgroundColor: '#1e293b',
              borderRadius: '12px',
              border: '1px solid #334155',
              overflow: 'hidden'
            }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ backgroundColor: '#0f172a', borderBottom: '1px solid #334155', color: '#94a3b8' }}>
                    <th style={{ padding: '1rem 1.25rem' }}>Event Action</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Performed By</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Client IP</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Details</th>
                    <th style={{ padding: '1rem 1.25rem' }}>Timestamp</th>
                  </tr>
                </thead>
                <tbody>
                  {activityLogs.length === 0 ? (
                    <tr>
                      <td colSpan="5" style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>
                        No system activity audit logs recorded yet.
                      </td>
                    </tr>
                  ) : (
                    activityLogs.map((log) => (
                      <tr key={log.id} style={{ borderBottom: '1px solid #334155' }}>
                        <td style={{ padding: '0.85rem 1.25rem' }}>
                          <span style={{
                            padding: '0.2rem 0.5rem',
                            borderRadius: '4px',
                            fontSize: '0.75rem',
                            fontWeight: '700',
                            backgroundColor: `${getActionBadgeColor(log.action)}22`,
                            color: getActionBadgeColor(log.action)
                          }}>
                            {log.action}
                          </span>
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', color: '#e2e8f0', fontWeight: '500' }}>
                          {log.performedByEmail || 'System / Anonymous'}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', color: '#94a3b8', fontFamily: 'monospace' }}>
                          {log.clientIp}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', color: '#cbd5e1' }}>
                          {log.details}
                        </td>
                        <td style={{ padding: '0.85rem 1.25rem', color: '#94a3b8' }}>
                          {formatDate(log.timestamp)}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
