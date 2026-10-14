/// <reference types="cypress" />

describe('Sprint 4 Comprehensive E2E Test Suite - Admin Portal & Operations Hub', () => {

  // Common Mock Entities
  const mockAdminUser = {
    userId: 'usr-admin-1',
    id: 'usr-admin-1',
    email: 'admin@rescueplate.org',
    role: 'ADMIN',
    name: 'System Admin',
    businessName: 'RescuePlate Admin Central',
    phone: '+1 (555) 000-1111',
    location: 'RescuePlate HQ, New York, NY',
    token: 'mock_jwt_admin_token_sprint4'
  };

  const mockUsers = [
    {
      id: 'usr-admin-1',
      email: 'admin@rescueplate.org',
      role: 'ADMIN',
      businessName: 'RescuePlate Admin Central',
      location: 'RescuePlate HQ, New York, NY',
      createdAt: '2026-01-01T10:00:00.000Z',
      isActive: true
    },
    {
      id: 'usr-donor-1',
      email: 'sarah.jenkins@grandhorizon.org',
      role: 'DONOR',
      businessName: 'Grand Horizon Bakery & Cafe',
      location: '450 5th Ave, New York, NY',
      createdAt: '2026-02-15T14:30:00.000Z',
      isActive: true
    },
    {
      id: 'usr-org-1',
      email: 'david@communityhopeshelter.org',
      role: 'ORGANIZATION',
      businessName: 'Community Hope Shelter & Kitchen',
      location: '350 W 42nd St, New York, NY',
      createdAt: '2026-03-01T09:15:00.000Z',
      isActive: true
    },
    {
      id: 'usr-donor-inactive',
      email: 'inactive_donor@bistro.com',
      role: 'DONOR',
      businessName: 'Suspended Bistro',
      location: '12 Green St, Brooklyn, NY',
      createdAt: '2026-03-10T11:00:00.000Z',
      isActive: false
    }
  ];

  const mockOverviewData = {
    totalUsers: 4,
    activeDonors: 1,
    activeOrganizations: 1,
    donationSummary: {
      data: {
        status: 'AVAILABLE',
        totalCount: 24,
        availableCount: 15,
        claimedCount: 6,
        totalPortionsRescued: 340,
        totalPortionsListed: 450,
        expiredCount: 2,
        cancelledCount: 1
      }
    },
    requestSummary: {
      data: {
        status: 'AVAILABLE',
        totalCount: 18,
        pendingCount: 4,
        acceptedCount: 12,
        totalAcceptedQuantity: 280,
        rejectedCount: 2
      }
    },
    deliverySummary: {
      data: {
        status: 'AVAILABLE',
        totalCount: 12,
        arrangedCount: 5,
        collectedCount: 3,
        completedCount: 4,
        receivedCount: 4,
        cancelledCount: 0
      }
    }
  };

  const mockActivityLogs = [
    {
      id: 'log-101',
      action: 'ADMIN_LOGIN_SUCCESS',
      performedByEmail: 'admin@rescueplate.org',
      clientIp: '127.0.0.1',
      details: 'Administrator successfully authenticated via 2FA Access Key challenge.',
      timestamp: new Date().toISOString()
    },
    {
      id: 'log-102',
      action: 'USER_DEACTIVATED',
      performedByEmail: 'admin@rescueplate.org',
      clientIp: '127.0.0.1',
      details: 'User account inactive_donor@bistro.com was deactivated by administrator.',
      timestamp: new Date(Date.now() - 3600000).toISOString()
    },
    {
      id: 'log-103',
      action: 'USER_REACTIVATED',
      performedByEmail: 'admin@rescueplate.org',
      clientIp: '127.0.0.1',
      details: 'User account sarah.jenkins@grandhorizon.org was reactivated by administrator.',
      timestamp: new Date(Date.now() - 7200000).toISOString()
    }
  ];

  beforeEach(() => {
    cy.viewport(1280, 800);

    // Auto-accept browser confirm/alert dialogs (e.g. window.confirm)
    cy.on('window:confirm', () => true);
    cy.on('window:alert', () => true);

    // Global baseline API intercepts for peripheral endpoints
    cy.intercept('GET', '**/api/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/deliveries/notifications*', { statusCode: 200, body: { success: true, data: [] } });
  });

  // =========================================================================
  // SUITE 1: Two-Stage Administrator Authentication & Route Security
  // =========================================================================
  describe('Suite 1: Two-Stage Administrator Authentication & Route Security', () => {

    it('TC-ADM-01: Admin login interface renders branding, credential inputs, and return link', () => {
      cy.visit('/admin/login');

      // Verify header branding
      cy.contains('RescuePlate Admin Console').should('be.visible');
      cy.contains('Protected Administrator Control Portal').should('be.visible');

      // Verify Stage 1 inputs scoped to the admin login card
      cy.get('.admin-login-card input[type="email"]')
        .should('be.visible')
        .and('have.attr', 'placeholder', 'admin@rescueplate.org');
      cy.get('.admin-login-card input[type="password"]')
        .should('be.visible');
      cy.get('.admin-login-card').contains('button', 'Authenticate Credentials →').should('be.visible');

      // Verify return to public home page navigation link
      cy.contains('a', '← Return to Public Home Page')
        .should('be.visible')
        .and('have.attr', 'href', '/');

      cy.screenshot('tc-adm-01-admin-login-render');
    });

    it('TC-ADM-02: Stage 1 authentication rejects invalid administrator credentials', () => {
      cy.intercept('POST', '**/api/admin/login', {
        statusCode: 401,
        body: { success: false, message: 'Invalid administrator email or password.' }
      }).as('failedAdminLogin');

      cy.visit('/admin/login');

      cy.get('.admin-login-card input[type="email"]').type('unknown_admin@rescueplate.org');
      cy.get('.admin-login-card input[type="password"]').type('WrongPassword123!');
      cy.get('.admin-login-card').contains('button', 'Authenticate Credentials →').click();

      cy.wait('@failedAdminLogin');

      // Assert error alert is presented and user remains on Stage 1
      cy.contains('Invalid administrator email or password.').should('be.visible');
      cy.get('.admin-login-card').contains('button', 'Authenticate Credentials →').should('be.visible');

      cy.screenshot('tc-adm-02-stage1-rejection');
    });

    it('TC-ADM-03: Stage 1 authentication succeeds and transitions to Stage 2 PIN challenge', () => {
      cy.intercept('POST', '**/api/admin/login', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Credentials authenticated. Access key PIN required.',
          data: {
            challengeToken: 'valid-challenge-token-stage1',
            expiresInSeconds: 300,
            requiresAccessKey: true
          }
        }
      }).as('stage1Success');

      cy.visit('/admin/login');

      cy.get('.admin-login-card input[type="email"]').type('admin@rescueplate.org');
      cy.get('.admin-login-card input[type="password"]').type('Admin@123');
      cy.get('.admin-login-card').contains('button', 'Authenticate Credentials →').click();

      cy.wait('@stage1Success');

      // Assert transition to Stage 2 PIN verification
      cy.contains('Step 2 Verification Active').should('be.visible');
      cy.contains('Authenticated as admin@rescueplate.org').should('be.visible');
      cy.contains('⏱ 5:00').should('be.visible');
      cy.get('.admin-login-card input[placeholder*="Access Key"]').should('be.visible');
      cy.get('.admin-login-card').contains('button', 'Verify Access Key ✓').should('be.visible');
      cy.get('.admin-login-card').contains('button', '← Back').should('be.visible');

      cy.screenshot('tc-adm-03-stage2-pin-challenge');
    });

    it('TC-ADM-04: Stage 2 challenge rejects incorrect access key PIN', () => {
      cy.intercept('POST', '**/api/admin/login', {
        statusCode: 200,
        body: {
          success: true,
          data: { challengeToken: 'test-challenge-token-invalid', expiresInSeconds: 300, requiresAccessKey: true }
        }
      }).as('stage1Pass');

      cy.intercept('POST', '**/api/admin/verify-access-key', {
        statusCode: 400,
        body: { success: false, message: 'Invalid Administrator Access Key.' }
      }).as('invalidKeyVerify');

      cy.visit('/admin/login');

      cy.get('.admin-login-card input[type="email"]').type('admin@rescueplate.org');
      cy.get('.admin-login-card input[type="password"]').type('Admin@123');
      cy.get('.admin-login-card').contains('button', 'Authenticate Credentials →').click();
      cy.wait('@stage1Pass');

      // Enter incorrect access key
      cy.get('.admin-login-card input[placeholder*="Access Key"]').type('WRONG-PIN-1234');
      cy.get('.admin-login-card').contains('button', 'Verify Access Key ✓').click();
      cy.wait('@invalidKeyVerify');

      // Assert error alert
      cy.contains('Invalid Administrator Access Key.').should('be.visible');

      cy.screenshot('tc-adm-04-stage2-pin-rejection');
    });

    it('TC-ADM-05: Stage 2 challenge succeeds with valid access key and grants dashboard entry', () => {
      cy.intercept('POST', '**/api/admin/login', {
        statusCode: 200,
        body: {
          success: true,
          data: { challengeToken: 'valid-challenge-token-pass', expiresInSeconds: 300, requiresAccessKey: true }
        }
      }).as('loginStage1');

      cy.intercept('POST', '**/api/admin/verify-access-key', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Administrator access granted!',
          data: mockAdminUser
        }
      }).as('loginStage2');

      // Mock dashboard endpoints
      cy.intercept('GET', '**/api/admin/users', { statusCode: 200, body: { success: true, data: mockUsers } }).as('getUsers');
      cy.intercept('GET', '**/api/admin/monitoring/overview', { statusCode: 200, body: { success: true, data: mockOverviewData } }).as('getOverview');
      cy.intercept('GET', '**/api/admin/monitoring/activity*', { statusCode: 200, body: { success: true, data: mockActivityLogs } }).as('getLogs');

      cy.visit('/admin/login');

      cy.get('.admin-login-card input[type="email"]').type('admin@rescueplate.org');
      cy.get('.admin-login-card input[type="password"]').type('Admin@123');
      cy.get('.admin-login-card').contains('button', 'Authenticate Credentials →').click();
      cy.wait('@loginStage1');

      cy.get('.admin-login-card input[placeholder*="Access Key"]').type('ADMIN-SECURE-KEY-2026');
      cy.get('.admin-login-card').contains('button', 'Verify Access Key ✓').click();
      cy.wait('@loginStage2');

      // Verify redirection and dashboard load
      cy.url().should('include', '/admin/dashboard');
      cy.contains('VERIFIED ADMIN').should('be.visible');
      cy.contains('System Administration Hub').should('be.visible');
      cy.contains('admin@rescueplate.org').should('be.visible');

      cy.screenshot('tc-adm-05-successful-admin-login');
    });

    it('TC-ADM-06: Unauthenticated user accessing /admin/dashboard is redirected to /admin/login', () => {
      // Ensure local storage has no auth tokens
      cy.clearLocalStorage();

      cy.visit('/admin/dashboard');

      // Route guard immediately redirects to /admin/login
      cy.url().should('include', '/admin/login');
      cy.contains('RescuePlate Admin Console').should('be.visible');

      cy.screenshot('tc-adm-06-unauthenticated-guard-redirect');
    });

    it('TC-ADM-07: Non-admin authenticated user receives 403 Administrative Access Denied screen', () => {
      // Authenticate as normal DONOR
      cy.loginAsDonor();

      cy.visit('/admin/dashboard');

      // Verify 403 Access Denied screen
      cy.contains('403 — Administrative Access Denied').should('be.visible');
      cy.contains('does not possess System Administrator privileges').should('be.visible');
      cy.contains('a', 'Return to Public Portal')
        .should('be.visible')
        .and('have.attr', 'href', '/');

      cy.screenshot('tc-adm-07-donor-403-forbidden');
    });

  });

  // =========================================================================
  // SUITE 2: Platform Overview & Microservices Telemetry
  // =========================================================================
  describe('Suite 2: Platform Overview & Microservices Telemetry', () => {

    beforeEach(() => {
      cy.loginAsAdmin();

      cy.intercept('GET', '**/api/admin/users', { statusCode: 200, body: { success: true, data: mockUsers } }).as('getUsers');
      cy.intercept('GET', '**/api/admin/monitoring/overview', { statusCode: 200, body: { success: true, data: mockOverviewData } }).as('getOverview');
      cy.intercept('GET', '**/api/admin/monitoring/activity*', { statusCode: 200, body: { success: true, data: mockActivityLogs } }).as('getLogs');

      cy.visit('/admin/dashboard');
      cy.wait('@getUsers');
      cy.wait('@getOverview');
      cy.wait('@getLogs');
    });

    it('TC-ADM-08: Executive Overview renders platform-wide summary metric KPI cards', () => {
      // Switch to Platform Overview tab
      cy.contains('button', 'Platform Overview').click();

      // Verify KPI Metric Cards
      cy.contains('TOTAL REGISTERED USERS').should('be.visible');
      cy.contains('4').should('be.visible');

      cy.contains('ACTIVE FOOD DONORS').should('be.visible');
      cy.contains('1').should('be.visible');

      cy.contains('CHARITY ORGANIZATIONS').should('be.visible');

      cy.contains('SYSTEM HEALTH STATUS').should('be.visible');
      cy.contains('● ONLINE & SECURE').should('be.visible');

      cy.screenshot('tc-adm-08-executive-kpi-cards');
    });

    it('TC-ADM-09: Microservices telemetry indicators display health status across all domains', () => {
      cy.contains('button', 'Platform Overview').click();

      cy.contains('Microservices Telemetry & Domain Boundaries').should('be.visible');

      // Verify each individual service domain card
      cy.contains('UserService (Port 5000)').should('be.visible');
      cy.contains('DonationService (Port 5001)').should('be.visible');
      cy.contains('RequestWorkflowService (Port 5002)').should('be.visible');
      cy.contains('DeliveryWorkflowService (Port 5003)').should('be.visible');

      cy.screenshot('tc-adm-09-microservices-telemetry');
    });

    it('TC-ADM-10: Cross-service aggregated telemetry renders donations, claims, and delivery metrics', () => {
      cy.contains('button', 'Platform Overview').click();

      // Surplus Food Donations card
      cy.contains('Surplus Food Donations').should('be.visible');
      cy.contains('Total Listings').should('be.visible');
      cy.contains('24').should('be.visible');
      cy.contains('Active / Available').should('be.visible');
      cy.contains('15').should('be.visible');
      cy.contains('Portions Rescued').should('be.visible');
      cy.contains('340').should('be.visible');

      // Charity Claims card
      cy.contains('Charity Claims & Requests').should('be.visible');
      cy.contains('Total Requests').should('be.visible');
      cy.contains('18').should('be.visible');
      cy.contains('Accepted Requests').should('be.visible');
      cy.contains('12').should('be.visible');

      // Delivery Telemetry card
      cy.contains('Logistics & Deliveries').should('be.visible');
      cy.contains('Arrangements').should('be.visible');
      cy.contains('12').should('be.visible');
      cy.contains('Food Collected').should('be.visible');
      cy.contains('Completed').should('be.visible');

      cy.screenshot('tc-adm-10-cross-service-metrics');
    });

  });

  // =========================================================================
  // SUITE 3: User Directory Management & Administrative Control
  // =========================================================================
  describe('Suite 3: User Directory Management & Administrative Control', () => {

    beforeEach(() => {
      cy.loginAsAdmin();

      cy.intercept('GET', '**/api/admin/users', { statusCode: 200, body: { success: true, data: mockUsers } }).as('getUsers');
      cy.intercept('GET', '**/api/admin/monitoring/overview', { statusCode: 200, body: { success: true, data: mockOverviewData } }).as('getOverview');
      cy.intercept('GET', '**/api/admin/monitoring/activity*', { statusCode: 200, body: { success: true, data: mockActivityLogs } }).as('getLogs');

      cy.visit('/admin/dashboard');
      cy.wait('@getUsers');
    });

    it('TC-ADM-11: User Directory tab renders comprehensive registered user table', () => {
      // Active tab defaults to 'users'
      cy.contains('button', 'User Directory & Control').should('be.visible');

      // Verify Table Columns
      cy.contains('th', 'Account Email').should('be.visible');
      cy.contains('th', 'Role').should('be.visible');
      cy.contains('th', 'Business / Org Name').should('be.visible');
      cy.contains('th', 'Location').should('be.visible');
      cy.contains('th', 'Registered Date').should('be.visible');
      cy.contains('th', 'Status').should('be.visible');
      cy.contains('th', 'Actions').should('be.visible');

      // Verify Rendered Users
      cy.contains('admin@rescueplate.org').should('be.visible');
      cy.contains('sarah.jenkins@grandhorizon.org').should('be.visible');
      cy.contains('david@communityhopeshelter.org').should('be.visible');
      cy.contains('inactive_donor@bistro.com').should('be.visible');

      cy.screenshot('tc-adm-11-user-directory-table');
    });

    it('TC-ADM-12: Live search filter accurately matches users by email, business name, or location', () => {
      const searchInput = 'input[placeholder*="Search users"]';

      // Search by business name
      cy.get(searchInput).type('Grand Horizon');
      cy.get('table tbody').should('contain', 'sarah.jenkins@grandhorizon.org');
      cy.get('table tbody').should('not.contain', 'david@communityhopeshelter.org');
      cy.get('table tbody').should('not.contain', 'inactive_donor@bistro.com');

      // Clear search
      cy.get(searchInput).clear();
      cy.get('table tbody').should('contain', 'david@communityhopeshelter.org');

      // Search by location
      cy.get(searchInput).type('Brooklyn');
      cy.get('table tbody').should('contain', 'inactive_donor@bistro.com');
      cy.get('table tbody').should('not.contain', 'sarah.jenkins@grandhorizon.org');

      cy.screenshot('tc-adm-12-live-search-filtering');
    });

    it('TC-ADM-13: Dropdown filters correctly isolate users by role and status', () => {
      // Filter by Role: DONOR
      cy.get('select').first().select('DONOR');
      cy.get('table tbody').should('contain', 'sarah.jenkins@grandhorizon.org');
      cy.get('table tbody').should('contain', 'inactive_donor@bistro.com');
      cy.get('table tbody').should('not.contain', 'david@communityhopeshelter.org');
      cy.get('table tbody').should('not.contain', 'admin@rescueplate.org');

      // Filter by Role: ORGANIZATION
      cy.get('select').first().select('ORGANIZATION');
      cy.get('table tbody').should('contain', 'david@communityhopeshelter.org');
      cy.get('table tbody').should('not.contain', 'sarah.jenkins@grandhorizon.org');

      // Reset Role filter to ALL
      cy.get('select').first().select('ALL');

      // Filter by Status: Inactive Only
      cy.get('select').eq(1).select('INACTIVE');
      cy.get('table tbody').should('contain', 'inactive_donor@bistro.com');
      cy.get('table tbody').should('not.contain', 'sarah.jenkins@grandhorizon.org');
      cy.get('table tbody').should('not.contain', 'david@communityhopeshelter.org');

      // Filter by Status: Active Only
      cy.get('select').eq(1).select('ACTIVE');
      cy.get('table tbody').should('contain', 'sarah.jenkins@grandhorizon.org');
      cy.get('table tbody').should('not.contain', 'inactive_donor@bistro.com');

      cy.screenshot('tc-adm-13-role-and-status-filters');
    });

    it('TC-ADM-14: Admin can deactivate and subsequently reactivate a user account with audit feedback', () => {
      // Step 1: Deactivate active user sarah.jenkins@grandhorizon.org
      cy.intercept('PATCH', '**/api/admin/users/usr-donor-1/status', {
        statusCode: 200,
        body: { success: true, message: 'User account successfully deactivated.' }
      }).as('deactivateUser');

      const updatedLogsAfterDeactivation = [
        {
          id: 'log-104',
          action: 'USER_DEACTIVATED',
          performedByEmail: 'admin@rescueplate.org',
          clientIp: '127.0.0.1',
          details: 'User account sarah.jenkins@grandhorizon.org was deactivated by administrator.',
          timestamp: new Date().toISOString()
        },
        ...mockActivityLogs
      ];

      cy.intercept('GET', '**/api/admin/monitoring/activity*', {
        statusCode: 200,
        body: { success: true, data: updatedLogsAfterDeactivation }
      }).as('getUpdatedLogs');

      // Find Sarah's row and click Deactivate
      cy.contains('tr', 'sarah.jenkins@grandhorizon.org').within(() => {
        cy.contains('button', 'Deactivate').click();
      });

      cy.wait('@deactivateUser');
      cy.wait('@getUpdatedLogs');

      // Verify success notification alert
      cy.contains('User account successfully deactivated.').should('be.visible');

      // Verify button transitioned to Reactivate and status is Inactive
      cy.contains('tr', 'sarah.jenkins@grandhorizon.org').within(() => {
        cy.contains('● Inactive').should('be.visible');
        cy.contains('button', 'Reactivate').should('be.visible');
      });

      // Step 2: Reactivate the account
      cy.intercept('PATCH', '**/api/admin/users/usr-donor-1/status', {
        statusCode: 200,
        body: { success: true, message: 'User account successfully activated.' }
      }).as('reactivateUser');

      cy.contains('tr', 'sarah.jenkins@grandhorizon.org').within(() => {
        cy.contains('button', 'Reactivate').click();
      });

      cy.wait('@reactivateUser');

      // Verify alert banner and button transitioned back to Deactivate
      cy.contains('User account successfully activated.').should('be.visible');
      cy.contains('tr', 'sarah.jenkins@grandhorizon.org').within(() => {
        cy.contains('● Active').should('be.visible');
        cy.contains('button', 'Deactivate').should('be.visible');
      });

      cy.screenshot('tc-adm-14-deactivate-reactivate-flow');
    });

    it('TC-ADM-15: Protected Admin accounts cannot be deactivated and display immutability status', () => {
      // Find row for admin@rescueplate.org
      cy.contains('tr', 'admin@rescueplate.org').within(() => {
        cy.contains('ADMIN').should('be.visible');
        cy.contains('Protected Admin').should('be.visible');
        // Assert there is no Deactivate button for administrator
        cy.get('button').should('not.exist');
      });

      cy.screenshot('tc-adm-15-protected-admin-account');
    });

  });

  // =========================================================================
  // SUITE 4: System Audit Trail & Activity Logs
  // =========================================================================
  describe('Suite 4: System Audit Trail & Activity Logs', () => {

    beforeEach(() => {
      cy.loginAsAdmin();

      cy.intercept('GET', '**/api/admin/users', { statusCode: 200, body: { success: true, data: mockUsers } }).as('getUsers');
      cy.intercept('GET', '**/api/admin/monitoring/overview', { statusCode: 200, body: { success: true, data: mockOverviewData } }).as('getOverview');
      cy.intercept('GET', '**/api/admin/monitoring/activity*', { statusCode: 200, body: { success: true, data: mockActivityLogs } }).as('getLogs');

      cy.visit('/admin/dashboard');
      cy.wait('@getUsers');
    });

    it('TC-ADM-16: Activity logs tab displays chronological audit trail with action badges and metadata', () => {
      // Switch to System Activity & Audit Trail tab
      cy.contains('button', 'System Activity & Audit Trail').click();

      // Verify Audit Table Headers
      cy.contains('th', 'Event Action').should('be.visible');
      cy.contains('th', 'Performed By').should('be.visible');
      cy.contains('th', 'Client IP').should('be.visible');
      cy.contains('th', 'Details').should('be.visible');
      cy.contains('th', 'Timestamp').should('be.visible');

      // Verify Log Rows and Badges
      cy.contains('ADMIN_LOGIN_SUCCESS').should('be.visible');
      cy.contains('USER_DEACTIVATED').should('be.visible');
      cy.contains('USER_REACTIVATED').should('be.visible');
      cy.contains('127.0.0.1').should('be.visible');
      cy.contains('Administrator successfully authenticated via 2FA Access Key challenge.').should('be.visible');

      cy.screenshot('tc-adm-16-audit-activity-logs');
    });

    it('TC-ADM-17: Refresh telemetry button triggers live updates and re-queries monitoring data', () => {
      // Intercept on refresh
      cy.intercept('GET', '**/api/admin/users', { statusCode: 200, body: { success: true, data: mockUsers } }).as('refreshUsers');
      cy.intercept('GET', '**/api/admin/monitoring/overview', { statusCode: 200, body: { success: true, data: mockOverviewData } }).as('refreshOverview');
      cy.intercept('GET', '**/api/admin/monitoring/activity*', { statusCode: 200, body: { success: true, data: mockActivityLogs } }).as('refreshLogs');

      // Click Refresh Telemetry button in top banner
      cy.contains('button', 'Refresh Telemetry').click();

      cy.wait('@refreshUsers');
      cy.wait('@refreshOverview');
      cy.wait('@refreshLogs');

      // Confirm dashboard state remains intact and healthy
      cy.contains('System Administration Hub').should('be.visible');
      cy.contains('admin@rescueplate.org').should('be.visible');

      cy.screenshot('tc-adm-17-refresh-telemetry');
    });

  });

});
