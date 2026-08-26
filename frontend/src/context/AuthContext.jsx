import React, { createContext, useContext, useState, useEffect } from 'react';
import { authApi } from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [currentUser, setCurrentUser] = useState(() => {
    const saved = localStorage.getItem('rescueplate_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [loading, setLoading] = useState(false);

  const login = async (email, password) => {
    setLoading(true);
    try {
      // Connect directly to ASP.NET Core Backend API & PostgreSQL Database
      const res = await authApi.login({ email, password });
      if (res.success && res.data) {
        localStorage.setItem('rescueplate_token', res.data.token);
        localStorage.setItem('rescueplate_user', JSON.stringify(res.data));
        setCurrentUser(res.data);
        return { success: true, message: res.message || 'Signed in successfully!', user: res.data };
      }
      throw new Error(res.message || 'Invalid email or password.');
    } catch (err) {
      const errorMsg = err.message || err.error || 'Unable to connect to backend server. Make sure PostgreSQL and Backend are running.';
      throw new Error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const register = async (registerData) => {
    setLoading(true);
    try {
      // Save directly to PostgreSQL database via Backend API
      const res = await authApi.register(registerData);
      if (res.success && res.data) {
        localStorage.setItem('rescueplate_token', res.data.token);
        localStorage.setItem('rescueplate_user', JSON.stringify(res.data));
        setCurrentUser(res.data);
        return { success: true, message: res.message || 'Account registered and saved to database!', user: res.data };
      }
      throw new Error(res.message || 'Registration failed');
    } catch (err) {
      const errorMsg = err.message || err.error || 'Registration failed. Make sure email is unique and backend is running.';
      throw new Error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    authApi.logout();
    setCurrentUser(null);
  };

  const updateProfile = async (updateData) => {
    setLoading(true);
    try {
      const res = await authApi.updateMyProfile(updateData);
      if (res.success && res.data) {
        const updatedUser = {
          ...currentUser,
          businessName: res.data.businessOrOrgName || currentUser.businessName,
          name: res.data.contactName || currentUser.name
        };
        localStorage.setItem('rescueplate_user', JSON.stringify(updatedUser));
        setCurrentUser(updatedUser);
        return { success: true, message: 'Profile updated in database!', data: res.data };
      }
      throw new Error(res.message || 'Failed to update profile.');
    } finally {
      setLoading(false);
    }
  };

  const deleteAccount = async (password) => {
    setLoading(true);
    try {
      const res = await authApi.deleteAccount(password);
      logout();
      return { success: true, message: res.message || 'Your account has been deleted from database.' };
    } catch (err) {
      const msg = err.message || err.error || 'Failed to delete account. Please verify your password.';
      throw new Error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthContext.Provider value={{ currentUser, login, register, logout, updateProfile, deleteAccount, loading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
