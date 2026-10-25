/// <reference types="cypress" />

describe('Sprint 3 Comprehensive E2E Test Suite - Request Workflow Service (Food Needs, Offers, Approvals & Bulk Rejections)', () => {

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

  const initialNeedRequests = [
    {
      id: 501,
      organizationId: 'usr-org-1',
      organizationName: 'Community Hope Shelter & Kitchen',
      title: 'Urgently Requesting Fresh Produce Crates',
      category: 'Fresh Produce',
      quantityNeeded: 40,
      remainingNeeded: 40,
      fulfilledQuantity: 0,
      unit: 'portions',
      urgencyLevel: 'High',
      location: '350 W 42nd St, New York, NY',
      description: 'Needed for evening hot meal feeding 250 residents tonight.',
      status: 'OPEN',
      neededByDate: new Date(Date.now() + 12 * 3600000).toISOString(),
      createdAt: new Date().toISOString(),
      totalOffersCount: 1
    }
  ];

  const initialOffers = [
    {
      id: 601,
      needRequestId: 501,
      orgFoodNeedRequestTitle: 'Urgently Requesting Fresh Produce Crates',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      foodType: 'Fresh Apples & Oranges Crate',
      offeredQuantity: 15,
      unit: 'portions',
      notes: 'High quality surplus farm fresh crates available for pickup.',
      status: 'PENDING',
      createdAt: new Date().toISOString()
    },
    {
      id: 602,
      needRequestId: 501,
      orgFoodNeedRequestTitle: 'Urgently Requesting Fresh Produce Crates',
      donorId: 'usr-donor-2',
      donorName: 'Midtown Market',
      foodType: 'Carrots and Potatoes',
      offeredQuantity: 10,
      unit: 'portions',
      notes: 'Root vegetables ready for soup preparation.',
      status: 'PENDING',
      createdAt: new Date().toISOString()
    }
  ];

  const initialPortionRequests = [
    {
      id: 301,
      donationId: 101,
      donationTitle: 'Artisan Baguettes & Rye Bread',
      organizationId: 'usr-org-1',
      organizationName: 'Community Hope Shelter & Kitchen',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      requestedQuantity: 15,
      unit: 'portions',
      status: 'PENDING',
      message: 'Urgently needed for tonight dinner service at shelter.',
      createdAt: new Date().toISOString()
    },
    {
      id: 302,
      donationId: 101,
      donationTitle: 'Artisan Baguettes & Rye Bread',
      organizationId: 'usr-org-2',
      organizationName: 'St. Mary Youth Center',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      requestedQuantity: 10,
      unit: 'portions',
      status: 'PENDING',
      message: 'For youth weekend meal package distribution.',
      createdAt: new Date().toISOString()
    }
  ];

  beforeEach(() => {
    cy.viewport(1280, 800);
    cy.on('window:confirm', () => true);
    cy.on('window:alert', () => true);

    // Baseline notifications intercepts
    cy.intercept('GET', '**/api/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/deliveries/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/deliveries/request/*', { statusCode: 404, body: { success: false, message: 'Not found' } });
  });

  // =========================================================================
  // SUITE 1: Charity Food Needs Creation & Board
  // =========================================================================
  describe('Suite 1: Charity Food Needs Demands & Board', () => {

    it('TC-REQ-01: Charity views Food Needs page and opens Create Need Request modal', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getMyNeeds');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      cy.contains('Charity Food Needs & Offers').should('be.visible');
      cy.contains('button', 'Raise Food Need').click({ force: true });
      cy.contains('Raise Food Need Request').should('be.visible');
      cy.get('input[placeholder*="Urgent Need"]').should('be.visible');

      // Cancel button closes modal
      cy.contains('button', 'Cancel').click({ force: true });
      cy.contains('Raise Food Need Request').should('not.exist');
    });

    it('TC-REQ-02: Charity successfully submits urgent Food Need Request', () => {
      cy.loginAsCharity();

      const createdNeed = {
        id: 502,
        organizationId: 'usr-org-1',
        organizationName: 'Community Hope Shelter & Kitchen',
        title: 'Emergency Warm Soup Portions for Freeze Warning',
        category: 'Cooked Meals',
        quantityNeeded: 60,
        remainingNeeded: 60,
        unit: 'portions',
        location: '350 W 42nd St, New York, NY',
        neededByDate: new Date(Date.now() + 24 * 3600000).toISOString(),
        description: 'Severe weather advisory tonight; sheltering 120 guests.',
        status: 'OPEN',
        createdAt: new Date().toISOString(),
        totalOffersCount: 0
      };

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getMyNeeds');

      cy.intercept('POST', '**/api/need-requests', {
        statusCode: 201,
        body: {
          success: true,
          message: 'Food need request published successfully!',
          data: createdNeed
        }
      }).as('publishNeed');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      // Open Modal
      cy.contains('button', 'Raise Food Need').click({ force: true });

      // Fill in details
      cy.get('input[placeholder*="Urgent Need"]').type('Emergency Warm Soup Portions for Freeze Warning');
      cy.get('input[placeholder*="Community Center"]').clear().type('350 W 42nd St, New York, NY');
      
      const futureDate = new Date(Date.now() + 86400000).toISOString().slice(0, 16);
      cy.get('input[type="datetime-local"]').type(futureDate);
      cy.get('textarea[placeholder*="dietary requirements"]').type('Severe weather advisory tonight; sheltering 120 guests.');

      // Submit
      cy.contains('button', 'Publish Food Need').click({ force: true });
      cy.wait('@publishNeed');

      cy.contains('Food need request raised successfully!').should('be.visible');
    });

    it('TC-REQ-03: Created Food Need Request appears on Demands board with category and quantities', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: initialNeedRequests }
      }).as('getMyNeeds');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      cy.contains('Urgently Requesting Fresh Produce Crates').should('be.visible');
      cy.contains('0 / 40 portions').should('be.visible');
    });

  });

  // =========================================================================
  // SUITE 2: Donor Food Offers & Matching
  // =========================================================================
  describe('Suite 2: Donor Food Offers Lifecycle', () => {

    it('TC-REQ-04: Donor views open charity need and submits matching food offer', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/need-requests/active*', {
        statusCode: 200,
        body: { success: true, data: initialNeedRequests }
      }).as('getActiveNeeds');

      cy.intercept('POST', '**/api/need-requests/501/offers', {
        statusCode: 201,
        body: {
          success: true,
          message: 'Food offer submitted successfully! The organization has been notified.',
          data: initialOffers[0]
        }
      }).as('submitOffer');

      cy.visit('/food-needs');
      cy.wait('@getActiveNeeds');

      cy.contains('Urgently Requesting Fresh Produce Crates').should('be.visible');

      // Click "Offer Food to Charity" button on card
      cy.contains('button', 'Offer Food to Charity').first().click({ force: true });
      cy.contains('Offer Food to Charity').should('be.visible');

      // Fill in offer
      cy.get('input[placeholder*="Cooked Rice"]').type('Fresh Apples & Oranges Crate');
      cy.get('textarea[placeholder*="pickup by 6 PM"]').type('High quality surplus farm fresh crates available for pickup.');

      // Submit
      cy.contains('button', 'Submit Food Offer').click({ force: true });
      cy.wait('@submitOffer');

      cy.contains('Food offer submitted to Community Hope Shelter & Kitchen! Pending organization review.').should('be.visible');
    });

    it('TC-REQ-05: Donor verifies submitted offer appears in "My Submitted Food Offers" tab', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/need-requests/active*', {
        statusCode: 200,
        body: { success: true, data: initialNeedRequests }
      });

      cy.intercept('GET', '**/api/offers/my-offers*', {
        statusCode: 200,
        body: { success: true, data: [initialOffers[0]] }
      }).as('getMyOffers');

      cy.visit('/food-needs');

      // Switch to "My Submitted Food Offers" tab
      cy.contains('button', 'My Submitted Food Offers').click({ force: true });
      cy.wait('@getMyOffers');

      cy.contains('Fresh Apples & Oranges Crate').should('be.visible');
      cy.contains('15 portions').should('be.visible');
      cy.contains('Pending Review').should('be.visible');
    });

    it('TC-REQ-06: Charity opens Offers Received modal and accepts donor food offer', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: initialNeedRequests }
      }).as('getMyNeeds');

      cy.intercept('GET', '**/api/need-requests/501/offers', {
        statusCode: 200,
        body: { success: true, data: initialOffers }
      }).as('getOffersForNeed');

      const acceptedOffer = { ...initialOffers[0], status: 'ACCEPTED' };
      cy.intercept('POST', '**/api/offers/601/accept', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Donor offer accepted successfully! Delivery can now be scheduled.',
          data: acceptedOffer
        }
      }).as('acceptOffer');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      // Click Offers button
      cy.contains('button', 'Offers').first().click({ force: true });
      cy.wait('@getOffersForNeed');

      cy.contains('Offers Received').should('be.visible');
      cy.contains('Fresh Apples & Oranges Crate').should('be.visible');

      // Accept Offer
      cy.contains('button', 'Accept Offer').first().click({ force: true });
      cy.wait('@acceptOffer');

      cy.contains('Donor offer accepted successfully!').should('be.visible');
    });

    it('TC-REQ-07: Charity bulk rejects all pending offers for an organization food need', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: initialNeedRequests }
      }).as('getMyNeeds');

      cy.intercept('GET', '**/api/need-requests/501/offers', {
        statusCode: 200,
        body: { success: true, data: initialOffers }
      }).as('getOffersForNeed');

      cy.intercept('POST', '**/api/need-requests/501/offers/reject-all-pending', {
        statusCode: 200,
        body: {
          success: true,
          message: 'All pending offers declined successfully. Donors have been notified.',
          data: { rejectedCount: 2 }
        }
      }).as('bulkRejectOffers');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      cy.contains('button', 'Offers').first().click({ force: true });
      cy.wait('@getOffersForNeed');

      // Click "Reject All Pending Offers"
      cy.contains('button', 'Reject All Pending Offers').click({ force: true });
      cy.contains('Decline all remaining pending donor offers').should('be.visible');

      // Fill reason and confirm
      cy.get('textarea[placeholder*="fully fulfilled"]').type('This food demand was satisfied through local direct donations.');
      cy.contains('button', 'Confirm Reject All').click({ force: true });
      cy.wait('@bulkRejectOffers');

      cy.contains('All pending offers declined successfully').should('be.visible');
    });

  });

  // =========================================================================
  // SUITE 3: Surplus Food Portion Requests Workflow
  // =========================================================================
  describe('Suite 3: Surplus Food Portion Requests Lifecycle', () => {

    it('TC-REQ-08: Donor views received portion requests on Requests page and accepts charity claim', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: initialPortionRequests }
      }).as('getReceivedRequests');

      const acceptedReq = { ...initialPortionRequests[0], status: 'ACCEPTED' };
      cy.intercept('POST', '**/api/requests/301/accept', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Donation request accepted successfully!',
          data: acceptedReq
        }
      }).as('acceptRequest');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');

      cy.contains('Artisan Baguettes & Rye Bread').should('be.visible');
      cy.contains('Community Hope Shelter & Kitchen').should('be.visible');

      // Accept request
      cy.contains('button', 'Accept Request').first().click({ force: true });
      cy.wait('@acceptRequest');

      cy.contains('Donation request accepted successfully!').should('be.visible');
    });

    it('TC-REQ-09: Donor declines individual portion request with custom rejection message', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: initialPortionRequests }
      }).as('getReceivedRequests');

      cy.intercept('POST', '**/api/requests/*/reject', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Donation request declined.',
          data: { ...initialPortionRequests[0], status: 'REJECTED' }
        }
      }).as('rejectRequest');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');

      // Click Decline
      cy.contains('button', 'Decline').first().click({ force: true });
      cy.contains('Decline Donation Request').should('be.visible');

      // Enter decline reason
      cy.get('textarea[placeholder*="Remaining quantity"]').type('Unfortunately current batch has reached reservation limit.');
      cy.contains('button', 'Confirm Decline').click({ force: true });
      cy.wait('@rejectRequest');

      cy.contains('Donation request declined.').should('be.visible');
    });

    it('TC-REQ-10: Donor bulk rejects all pending requests for a surplus donation listing', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: initialPortionRequests }
      }).as('getReceivedRequests');

      cy.intercept('POST', '**/api/requests/donations/101/reject-all-pending', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Bulk rejected 2 pending request(s) for this donation listing.',
          data: { rejectedCount: 2 }
        }
      }).as('bulkRejectDonationRequests');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');

      // Click "Reject All Pending"
      cy.contains('button', 'Reject All Pending').first().click({ force: true });
      cy.contains('Reject All Pending Requests').should('be.visible');

      // Provide custom reason
      cy.get('textarea[placeholder*="fully claimed"]').type('Donation fully claimed by shelter batch.');
      cy.contains('button', 'Confirm Reject All').click({ force: true });
      cy.wait('@bulkRejectDonationRequests');

      cy.contains('Bulk rejected 2 pending request(s)').should('be.visible');
    });

  });

});
