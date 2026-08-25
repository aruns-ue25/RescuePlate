import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import Navbar from './components/Navbar';
import Footer from './components/Footer';
import ProtectedRoute from './components/ProtectedRoute';

// Dedicated Pages
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import AboutPage from './pages/AboutPage';
import HowItWorksPage from './pages/HowItWorksPage';
import ContactPage from './pages/ContactPage';
import DonorDashboardPage from './pages/DonorDashboardPage';
import OrganizationBrowsePage from './pages/OrganizationBrowsePage';
import ProfilePage from './pages/ProfilePage';

import './App.css';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <div className="app-wrapper">
          {/* Header Navigation */}
          <Navbar />

          {/* Main Multi-Page Route Switcher */}
          <main className="main-content">
            <Routes>
              {/* Public Pages */}
              <Route path="/" element={<HomePage />} />
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/about" element={<AboutPage />} />
              <Route path="/how-it-works" element={<HowItWorksPage />} />
              <Route path="/contact" element={<ContactPage />} />
              <Route path="/browse-food" element={<OrganizationBrowsePage />} />

              {/* Protected Donor Portal (Only authenticated DONOR can access) */}
              <Route
                path="/donor-portal"
                element={
                  <ProtectedRoute allowedRoles={['DONOR']}>
                    <DonorDashboardPage />
                  </ProtectedRoute>
                }
              />

              {/* Protected User Profile (Any authenticated user) */}
              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <ProfilePage />
                  </ProtectedRoute>
                }
              />
            </Routes>
          </main>

          {/* Site Footer */}
          <Footer />
        </div>
      </BrowserRouter>
    </AuthProvider>
  );
}
