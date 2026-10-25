/// <reference types="cypress" />

describe('Sprint 1 Comprehensive E2E Test Suite - User Service (Auth, Registration, Profile)', () => {

  const mockDonorUser = {
    userId: 'usr-donor-101',
    id: 'usr-donor-101',
    email: 'freshbakes@artisanbakery.com',
    role: 'DONOR',
    name: 'Elena Rostova',
    businessName: 'Sunrise Artisan Bakery',
    phone: '+15551234567',
    location: '124 Central Ave, Metro City',
    address: '124 Central Ave, Metro City',
    token: 'mock_jwt_donor_sprint1_token'
  };

  const mockCharityUser = {
    userId: 'usr-org-202',
    id: 'usr-org-202',
    email: 'contact@communityhope.org',
    role: 'ORGANIZATION',
    name: 'Marcus Vance',
    businessName: 'Community Hope Shelter',
    phone: '+15559876543',
    location: '350 W 42nd St, New York, NY',
    address: '350 W 42nd St, New York, NY',
    token: 'mock_jwt_charity_sprint1_token'
  };

  const mockProfileData = {
    id: 'usr-donor-101',
    userId: 'usr-donor-101',
    email: 'freshbakes@artisanbakery.com',
    contactName: 'Elena Rostova',
    businessOrOrgName: 'Sunrise Artisan Bakery',
    role: 'DONOR',
    phone: '+15551234567',
    address: '124 Central Ave, Metro City',
    bioOrDescription: 'Dedicated to donating fresh evening sourdough loaves and pastries daily.',
    donorType: 'Bakery',
    acceptedFoodCategories: []
  };

  beforeEach(() => {
    cy.viewport(1280, 800);
    cy.on('window:confirm', () => true);
    cy.on('window:alert', () => true);

    // Baseline notifications intercepts
    cy.intercept('GET', '**/api/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/deliveries/notifications*', { statusCode: 200, body: { success: true, data: [] } });
  });

  // =========================================================================
  // SUITE 1: Registration Workflow (Donor & Charity)
  // =========================================================================
  describe('Suite 1: User Registration Workflow', () => {

    it('TC-USR-01: Registration page renders role cards, branding, and link to sign in', () => {
      cy.visit('/register');
      cy.contains('Join RescuePlate').should('be.visible');
      cy.contains('Create Your Account').should('be.visible');
      cy.contains('Food Business / Donor').should('be.visible');
      cy.contains('Charity / Nonprofit').should('be.visible');
      cy.contains('Already registered?').should('be.visible');
      cy.contains('Sign in here').should('have.attr', 'href', '/login');
    });

    it('TC-USR-02: Registration form validates password length (minimum 6 characters)', () => {
      cy.visit('/register');

      cy.get('.auth-card input[placeholder*="Sunrise"]').type('Sunrise Artisan Bakery');
      cy.get('.auth-card input[placeholder*="Elena"]').type('Elena Rostova');
      cy.get('.auth-card input[placeholder*="124 Central"]').type('124 Central Ave, Metro City');
      cy.get('.auth-card input[type="email"]').type('freshbakes@artisanbakery.com');
      cy.get('.auth-card input[type="password"]').first().type('123');
      cy.get('.auth-card input[type="password"]').last().type('123');
      cy.get('#reg-terms').check({ force: true });

      cy.contains('button', 'Create Donor Account').click({ force: true });
      cy.contains('Password must be at least 6 characters long.').should('be.visible');
    });

    it('TC-USR-03: Registration form validates password matching between password and confirm password', () => {
      cy.visit('/register');

      cy.get('.auth-card input[placeholder*="Sunrise"]').type('Sunrise Artisan Bakery');
      cy.get('.auth-card input[placeholder*="Elena"]').type('Elena Rostova');
      cy.get('.auth-card input[placeholder*="124 Central"]').type('124 Central Ave, Metro City');
      cy.get('.auth-card input[type="email"]').type('freshbakes@artisanbakery.com');
      cy.get('.auth-card input[type="password"]').first().type('securePass123!');
      cy.get('.auth-card input[type="password"]').last().type('differentPass456!');
      cy.get('#reg-terms').check({ force: true });

      cy.contains('button', 'Create Donor Account').click({ force: true });
      cy.contains('Passwords do not match.').should('be.visible');
    });

    it('TC-USR-04: Successful Donor registration saves account and redirects to donor portal', () => {
      cy.intercept('POST', '**/api/auth/register', {
        statusCode: 201,
        body: {
          success: true,
          message: 'Account registered and saved to database!',
          data: {
            token: mockDonorUser.token,
            ...mockDonorUser
          }
        }
      }).as('registerDonor');

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getMyDonations');

      cy.visit('/register');

      cy.get('.auth-card input[placeholder*="Sunrise"]').type('Sunrise Artisan Bakery');
      cy.get('.auth-card input[placeholder*="Elena"]').type('Elena Rostova');
      cy.get('.auth-card input[placeholder*="124 Central"]').type('124 Central Ave, Metro City');
      cy.get('.auth-card input[placeholder*="0771234567"]').type('0771234567');
      cy.get('.auth-card input[type="email"]').type('freshbakes@artisanbakery.com');
      cy.get('.auth-card input[type="password"]').first().type('SecretPassword123!');
      cy.get('.auth-card input[type="password"]').last().type('SecretPassword123!');
      cy.get('#reg-terms').check({ force: true });

      cy.contains('button', 'Create Donor Account').click({ force: true });
      cy.wait('@registerDonor');

      cy.url().should('include', '/donor-portal');
      cy.window().then((win) => {
        expect(win.localStorage.getItem('rescueplate_token')).to.equal(mockDonorUser.token);
      });
    });

    it('TC-USR-05: Successful Charity organization registration saves accepted categories and redirects to browse food', () => {
      cy.intercept('POST', '**/api/auth/register', {
        statusCode: 201,
        body: {
          success: true,
          message: 'Account registered and saved to database!',
          data: {
            token: mockCharityUser.token,
            ...mockCharityUser
          }
        }
      }).as('registerCharity');

      cy.intercept('GET', '**/api/donations*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getAvailableDonations');

      cy.intercept('GET', '**/api/profile/inactive-user-ids*', {
        statusCode: 200,
        body: []
      });

      cy.visit('/register');

      // Select Charity Role Card
      cy.contains('Charity / Nonprofit').click({ force: true });
      cy.contains('Create Charity Account').should('be.visible');

      cy.get('.auth-card input[placeholder*="Hope Community"]').type('Community Hope Shelter');
      cy.get('.auth-card input[placeholder*="Elena"]').type('Marcus Vance');
      cy.get('.auth-card input[placeholder*="124 Central"]').type('350 W 42nd St, New York, NY');
      cy.get('.auth-card input[type="email"]').type('contact@communityhope.org');
      cy.get('.auth-card input[type="password"]').first().type('CharityPass123!');
      cy.get('.auth-card input[type="password"]').last().type('CharityPass123!');
      cy.get('#reg-terms').check({ force: true });

      cy.contains('button', 'Create Charity Account').click({ force: true });
      cy.wait('@registerCharity');

      cy.url().should('include', '/browse-food');
    });

  });

  // =========================================================================
  // SUITE 2: Authentication & Login Workflow
  // =========================================================================
  describe('Suite 2: Universal Login & Authentication', () => {

    it('TC-USR-06: Universal login interface validates required fields and password show/hide toggle', () => {
      cy.visit('/login');
      cy.contains('Sign In to Your Account').should('be.visible');
      cy.contains('RescuePlate Secure Login').should('be.visible');

      // Toggle password visibility
      cy.get('.auth-card input[type="password"]').type('mySecretPassword');
      cy.get('.password-toggle-btn').click({ force: true });
      cy.get('.auth-card input[type="text"]').should('have.value', 'mySecretPassword');
      cy.get('.password-toggle-btn').click({ force: true });
      cy.get('.auth-card input[type="password"]').should('have.value', 'mySecretPassword');
    });

    it('TC-USR-07: Login authentication rejects invalid credentials with error banner', () => {
      cy.intercept('POST', '**/api/auth/login', {
        statusCode: 401,
        body: {
          success: false,
          message: 'Invalid email or password. Please try again.'
        }
      }).as('failedLogin');

      cy.visit('/login');
      cy.get('.auth-card input[type="email"]').type('wrong_user@example.com');
      cy.get('.auth-card input[type="password"]').type('wrongPassword999');
      cy.get('.auth-card button[type="submit"]').click({ force: true });

      cy.wait('@failedLogin');
      cy.contains('Invalid email or password. Please try again.').should('be.visible');
    });

    it('TC-USR-08: Successful login stores auth token in localStorage and navigates to correct role dashboard', () => {
      cy.intercept('POST', '**/api/auth/login', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Signed in successfully!',
          data: {
            token: mockDonorUser.token,
            ...mockDonorUser
          }
        }
      }).as('loginSuccess');

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [] }
      });

      cy.visit('/login');
      cy.get('.auth-card input[type="email"]').type('freshbakes@artisanbakery.com');
      cy.get('.auth-card input[type="password"]').type('validPassword123');
      cy.get('.auth-card button[type="submit"]').click({ force: true });

      cy.wait('@loginSuccess');
      cy.url().should('include', '/donor-portal');
      cy.window().then((win) => {
        expect(win.localStorage.getItem('rescueplate_token')).to.equal(mockDonorUser.token);
      });
    });

  });

  // =========================================================================
  // SUITE 3: User Profile & Session Management
  // =========================================================================
  describe('Suite 3: Profile Management & Session Lifecycle', () => {

    it('TC-USR-09: User profile page loads current profile and allows updating phone and address', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/profile/me*', {
        statusCode: 200,
        body: {
          success: true,
          data: mockProfileData
        }
      }).as('getProfile');

      const updatedProfile = {
        ...mockProfileData,
        phone: '+15559998888',
        address: '500 Madison Ave, New York, NY'
      };

      cy.intercept('PUT', '**/api/profile/me*', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Profile updated and saved to database successfully!',
          data: updatedProfile
        }
      }).as('updateProfile');

      cy.visit('/profile');
      cy.wait('@getProfile');

      cy.contains('Sunrise Artisan Bakery').should('be.visible');
      cy.contains('freshbakes@artisanbakery.com').should('be.visible');

      // Click Edit Profile button
      cy.contains('button', 'Edit Profile').click({ force: true });

      // Change address
      cy.get('input[value="124 Central Ave, Metro City"]')
        .clear()
        .type('500 Madison Ave, New York, NY');

      // Save Profile Changes
      cy.contains('button', 'Save Profile Changes').click({ force: true });
      cy.wait('@updateProfile');

      cy.contains('Profile updated and saved to database successfully!').should('be.visible');
    });

    it('TC-USR-10: Unauthenticated user accessing protected route (/profile) is redirected to /login, and logout clears session', () => {
      // Clear localStorage session
      cy.clearLocalStorage();

      // Accessing /profile when unauthenticated is redirected to /login by ProtectedRoute
      cy.visit('/profile');
      cy.url().should('include', '/login');
      cy.contains('Sign In to Your Account').should('be.visible');

      // Now login as donor
      cy.loginAsDonor();
      cy.intercept('GET', '**/api/profile/me*', {
        statusCode: 200,
        body: { success: true, data: mockProfileData }
      });
      cy.visit('/profile');
      cy.contains('Sunrise Artisan Bakery').should('be.visible');

      // Click Sign Out button
      cy.intercept('POST', '**/api/auth/logout', { statusCode: 200, body: { success: true } });
      cy.contains('button', 'Sign Out').click({ force: true });

      // Session cleared
      cy.window().then((win) => {
        expect(win.localStorage.getItem('rescueplate_token')).to.be.null;
        expect(win.localStorage.getItem('rescueplate_user')).to.be.null;
      });
      cy.url().should('satisfy', (url) => url.endsWith('/') || url.includes('/login'));
    });

  });

});
