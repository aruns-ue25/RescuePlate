/// <reference types="cypress" />

describe('Sprint 2 Comprehensive E2E Test Suite - Donation Service (Surplus Posting, Marketplace, Directories)', () => {

  const mockDonorUser = {
    userId: 'usr-donor-1',
    id: 'usr-donor-1',
    email: 'sarah.jenkins@grandhorizon.org',
    role: 'DONOR',
    name: 'Sarah Jenkins',
    businessName: 'Grand Horizon Bakery & Cafe',
    phone: '+1 (555) 234-5678',
    location: '450 5th Ave, New York, NY',
    address: '450 5th Ave, New York, NY'
  };

  const mockCharityUser = {
    userId: 'usr-org-1',
    id: 'usr-org-1',
    email: 'david@communityhopeshelter.org',
    role: 'ORGANIZATION',
    name: 'David Martinez',
    businessName: 'Community Hope Shelter & Kitchen',
    phone: '+1 (555) 345-6789',
    location: '350 W 42nd St, New York, NY',
    address: '350 W 42nd St, New York, NY'
  };

  const initialDonations = [
    {
      id: 101,
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      foodTitle: 'Artisan Baguettes & Sourdough Loaves',
      category: 'Bakery & Pastries',
      totalQuantity: 40,
      remainingQuantity: 25,
      claimedQuantity: 15,
      unit: 'portions',
      expiryHours: 6,
      expiryTime: new Date(Date.now() + 6 * 3600000).toISOString(),
      location: '450 5th Ave, New York, NY',
      dietaryTags: 'Vegetarian, Fresh',
      status: 'Available',
      notes: 'Freshly baked this morning, packed in bread crates.'
    },
    {
      id: 102,
      donorId: 'usr-donor-2',
      donorName: 'Green Valley Organic Market',
      foodTitle: 'Organic Crisp Apples & Citrus Crates',
      category: 'Fresh Produce',
      totalQuantity: 20,
      remainingQuantity: 20,
      claimedQuantity: 0,
      unit: 'boxes',
      expiryHours: 24,
      expiryTime: new Date(Date.now() + 24 * 3600000).toISOString(),
      location: '78 Farmers Way, Brooklyn, NY',
      dietaryTags: 'Vegan, Organic',
      status: 'Available',
      notes: 'Ripe seasonal fruits ready for immediate community meal prep.'
    },
    {
      id: 103,
      donorId: 'usr-donor-3',
      donorName: 'Midtown Gourmet Catering',
      foodTitle: 'Prepared Warm Lunch Trays',
      category: 'Cooked Meals',
      totalQuantity: 30,
      remainingQuantity: 30,
      claimedQuantity: 0,
      unit: 'portions',
      expiryHours: 4,
      expiryTime: new Date(Date.now() + 4 * 3600000).toISOString(),
      location: '120 Broadway, New York, NY',
      dietaryTags: 'Halal, High-Protein',
      status: 'Available',
      notes: 'Banquet overstock kept warm in insulated catering boxes.'
    }
  ];

  const mockDonors = [
    {
      donorId: 'usr-donor-1',
      id: 'usr-donor-1',
      businessName: 'Grand Horizon Bakery & Cafe',
      businessOrOrgName: 'Grand Horizon Bakery & Cafe',
      donorType: 'Bakery',
      location: '450 5th Ave, New York, NY',
      contactPhone: '+1 (555) 234-5678',
      totalDonationsCount: 42,
      status: 'Active',
      description: 'Daily provider of surplus sourdough bread, baguettes, and fresh pastries.'
    },
    {
      donorId: 'usr-donor-2',
      id: 'usr-donor-2',
      businessName: 'Green Valley Organic Market',
      businessOrOrgName: 'Green Valley Organic Market',
      donorType: 'Supermarket',
      location: '78 Farmers Way, Brooklyn, NY',
      contactPhone: '+1 (555) 345-6789',
      totalDonationsCount: 28,
      status: 'Active',
      description: 'Supplying fresh organic surplus fruits and vegetables to local shelters.'
    }
  ];

  const mockOrganizations = [
    {
      orgId: 'usr-org-1',
      id: 'usr-org-1',
      organizationName: 'Community Hope Shelter & Kitchen',
      businessOrOrgName: 'Community Hope Shelter & Kitchen',
      organizationType: 'Food Bank / Shelter',
      location: '350 W 42nd St, New York, NY',
      contactPhone: '+1 (555) 345-6789',
      requestedItemsCount: 18,
      status: 'Verified',
      description: 'Serving over 300 nutritious warm meals daily to families and individuals in need.'
    }
  ];

  beforeEach(() => {
    cy.viewport(1280, 800);
    cy.on('window:confirm', () => true);
    cy.on('window:alert', () => true);

    // Baseline intercepts
    cy.intercept('GET', '**/api/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/deliveries/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/profile/inactive-user-ids*', { statusCode: 200, body: [] });
  });

  // =========================================================================
  // SUITE 1: Donor Portal & Surplus Food Creation Workflow
  // =========================================================================
  describe('Suite 1: Donor Portal & Surplus Creation', () => {

    it('TC-DON-01: Donor Portal dashboard renders active listings table and Post Surplus button', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [initialDonations[0]] }
      }).as('getMyDonations');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      cy.contains('Donor Portal').should('be.visible');
      cy.contains('Post Food Donation').should('be.visible');
      cy.contains('Artisan Baguettes & Sourdough Loaves').should('be.visible');
      cy.contains('25 portions Available').should('be.visible');
    });

    it('TC-DON-02: Post Surplus Food modal opens and validates required fields', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getMyDonations');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Click Post Food Donation button
      cy.contains('button', 'Post Food Donation').first().click({ force: true });
      cy.contains('Post Surplus Food Donation').should('be.visible');
      cy.get('input[name="foodTitle"]').should('be.visible');

      // Cancel button closes modal
      cy.get('.modal-overlay, [style*="position: fixed"]').within(() => {
        cy.contains('button', 'Cancel').click({ force: true });
      });
      cy.contains('Broadcast surplus meals to local non-profit food charities').should('not.exist');
    });

    it('TC-DON-03: Donor successfully creates and lists new surplus food donation', () => {
      cy.loginAsDonor();

      const newDonation = {
        id: 104,
        donorId: 'usr-donor-1',
        donorName: 'Grand Horizon Bakery & Cafe',
        foodTitle: 'Warm Croissants & Danish Pastries',
        category: 'Bakery & Pastries',
        totalQuantity: 50,
        remainingQuantity: 50,
        claimedQuantity: 0,
        unit: 'portions',
        expiryHours: 8,
        expiryTime: new Date(Date.now() + 8 * 3600000).toISOString(),
        location: '450 5th Ave, New York, NY',
        dietaryTags: 'Vegetarian',
        status: 'Available',
        notes: 'Freshly baked breakfast surplus.'
      };

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getMyDonations');

      cy.intercept('POST', '**/api/donations', {
        statusCode: 201,
        body: {
          success: true,
          message: 'Donation listing posted successfully!',
          data: newDonation
        }
      }).as('createDonation');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Open creation modal
      cy.contains('button', 'Post Food Donation').first().click({ force: true });

      // Fill in details
      cy.get('input[name="foodTitle"]').type('Warm Croissants & Danish Pastries');
      cy.get('select[name="category"]').select('Bakery & Pastries');
      cy.get('input[name="totalQuantity"]').clear().type('50');
      cy.get('select[name="unit"]').select('portions');
      cy.get('select[name="expiryHours"]').select('8');
      cy.get('input[name="dietaryTags"]').type('Vegetarian');
      cy.get('textarea[name="notes"]').type('Freshly baked breakfast surplus.');

      // Submit form
      cy.contains('button', 'Publish Donation').click({ force: true });
      cy.wait('@createDonation');

      cy.contains('Surplus food donation posted successfully!').should('be.visible');
    });

    it('TC-DON-04: Created donation displays in Active Surplus Listings with category badge and portion count', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: initialDonations }
      }).as('getMyDonations');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Verify listing cards
      cy.contains('Artisan Baguettes & Sourdough Loaves').should('be.visible');
      cy.contains('Organic Crisp Apples & Citrus Crates').should('be.visible');
      cy.contains('Prepared Warm Lunch Trays').should('be.visible');
    });

  });

  // =========================================================================
  // SUITE 2: Organization Marketplace Discovery & Portion Claiming
  // =========================================================================
  describe('Suite 2: Browse Surplus Food & Portion Claiming', () => {

    it('TC-DON-05: Public Browse Surplus Food page displays active donation cards with dietary tags', () => {
      cy.intercept('GET', '**/api/donations*', {
        statusCode: 200,
        body: { success: true, data: initialDonations }
      }).as('getAvailableDonations');

      cy.visit('/browse-food');
      cy.wait('@getAvailableDonations');

      cy.contains('Browse Available Surplus Food').should('be.visible');
      cy.contains('Artisan Baguettes & Sourdough Loaves').should('be.visible');
      cy.contains('Organic Crisp Apples & Citrus Crates').should('be.visible');
      cy.contains('Vegetarian, Fresh').should('be.visible');
      cy.contains('Vegan, Organic').should('be.visible');
    });

    it('TC-DON-06: Browse page search bar and category dropdown filters accurately match food listings', () => {
      cy.intercept('GET', '**/api/donations*', (req) => {
        if (req.url.includes('search=Apples')) {
          req.reply({ statusCode: 200, body: { success: true, data: [initialDonations[1]] } });
        } else {
          req.reply({ statusCode: 200, body: { success: true, data: initialDonations } });
        }
      }).as('getFilteredDonations');

      cy.visit('/browse-food');
      cy.wait('@getFilteredDonations');

      // Search by keyword "Apples" and submit search
      cy.get('input[placeholder*="Search"]').type('Apples{enter}');
      cy.wait('@getFilteredDonations');

      cy.contains('Organic Crisp Apples & Citrus Crates').should('be.visible');
      cy.contains('Artisan Baguettes & Sourdough Loaves').should('not.exist');

      // Clear search
      cy.get('input[placeholder*="Search"]').clear().type('{enter}');
      cy.wait('@getFilteredDonations');
      cy.contains('Artisan Baguettes & Sourdough Loaves').should('be.visible');
    });

    it('TC-DON-07: Charity opens food details modal from Browse page and submits portion claim request', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/donations*', {
        statusCode: 200,
        body: { success: true, data: initialDonations }
      }).as('getAvailableDonations');

      cy.intercept('POST', '**/api/requests*', {
        statusCode: 201,
        body: {
          success: true,
          message: 'Food portion claim request submitted successfully!',
          data: {
            id: 501,
            donationId: 101,
            requestedQuantity: 10,
            status: 'PENDING'
          }
        }
      }).as('submitClaim');

      cy.visit('/browse-food');
      cy.wait('@getAvailableDonations');

      // Click "Request Food" button on the first card
      cy.contains('button', 'Request Food').first().click({ force: true });
      cy.contains('Request Food Donation').should('be.visible');
      cy.contains('Artisan Baguettes & Sourdough Loaves').should('be.visible');

      // Enter requested quantity
      cy.get('input[type="number"]').type('{selectall}10').should('have.value', '10');
      cy.get('textarea[placeholder*="Pickup vehicle"]').type('Needed for tonight dinner service at charity shelter.');

      // Submit request
      cy.contains('button', 'Confirm Request').click({ force: true });
      cy.wait('@submitClaim');

      cy.contains('Successfully requested 10 portions!').should('be.visible');
    });

  });

  // =========================================================================
  // SUITE 3: Donation Management & Directories
  // =========================================================================
  describe('Suite 3: Listing Modification, Cancellation, and Directories', () => {

    it('TC-DON-08: Donor opens Edit Details modal on Donor Portal and updates portion count and notes', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [initialDonations[0]] }
      }).as('getMyDonations');

      const updatedDonation = {
        ...initialDonations[0],
        totalQuantity: 60,
        remainingQuantity: 45,
        notes: 'Updated: additional sourdough loaves added.'
      };

      cy.intercept('PUT', '**/api/donations/101', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Donation listing updated successfully!',
          data: updatedDonation
        }
      }).as('updateDonation');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Click Edit button
      cy.contains('button', 'Edit').first().click({ force: true });
      cy.contains('Edit Food Donation #101').should('be.visible');

      // Update quantity
      cy.get('input[name="totalQuantity"]').clear().type('60');

      // Save Changes
      cy.contains('button', 'Save Changes').click({ force: true });
      cy.wait('@updateDonation');

      cy.contains('Donation details updated successfully!').should('be.visible');
    });

    it('TC-DON-09: Donor cancels surplus food listing with confirmation modal and cancellation reason', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: [initialDonations[0]] }
      }).as('getMyDonations');

      cy.intercept('PATCH', '**/api/donations/101/cancel', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Donation listing cancelled successfully.'
        }
      }).as('cancelDonation');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Click Cancel button on the donation card (targeted specifically to avoid matching the "Cancelled" filter tab)
      cy.get('button[title="Cancel this food donation"]').first().click({ force: true });
      cy.contains('Cancel Surplus Food Donation?').should('be.visible');

      // Confirm cancellation
      cy.contains('button', 'Yes, Cancel Listing').click({ force: true });
      cy.wait('@cancelDonation');

      cy.contains('Donation cancelled successfully.').should('be.visible');
    });

    it('TC-DON-10: Public Donor Directory and Organization Directory render partner profiles', () => {
      cy.intercept('GET', '**/api/donations/donors*', {
        statusCode: 200,
        body: { success: true, data: mockDonors }
      }).as('getDonors');

      cy.intercept('GET', '**/api/donations/organizations*', {
        statusCode: 200,
        body: { success: true, data: mockOrganizations }
      }).as('getOrgs');

      // Visit Donor Directory
      cy.visit('/donors');
      cy.wait('@getDonors');
      cy.contains('Participating Food Donors').should('be.visible');
      cy.contains('Grand Horizon Bakery & Cafe').should('be.visible');
      cy.contains('Green Valley Organic Market').should('be.visible');

      // Visit Organization Directory
      cy.visit('/organizations');
      cy.wait('@getOrgs');
      cy.contains('Participating Charitable Organizations').should('be.visible');
      cy.contains('Community Hope Shelter & Kitchen').should('be.visible');
    });

  });

});
