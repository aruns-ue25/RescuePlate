import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function ProtectedRoute({ children, allowedRoles }) {
  const { currentUser } = useAuth();
  const location = useLocation();

  // 1. If not logged in -> redirect to login page
  if (!currentUser) {
    return <Navigate to="/login" state={{ from: location, message: 'Please sign in to access this page.' }} replace />;
  }

  // 2. If role is not allowed -> redirect to appropriate portal
  if (allowedRoles && !allowedRoles.includes(currentUser.role)) {
    if (currentUser.role === 'DONOR') {
      return <Navigate to="/donor-portal" replace />;
    } else if (currentUser.role === 'ORGANIZATION') {
      return <Navigate to="/browse-food" replace />;
    } else {
      return <Navigate to="/profile" replace />;
    }
  }

  return children;
}
