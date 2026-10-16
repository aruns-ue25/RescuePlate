/// <reference types="cypress" />

describe('Sprint 4 Comprehensive E2E Test Suite - Delivery Workflow & Platform Ergonomics', () => {

  // Common Mock Entities
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

  const mockAdminUser = {
    userId: 'usr-admin-1',
    id: 'usr-admin-1',
    email: 'admin@rescueplate.org',
    role: 'ADMIN',
    name: 'System Operations Lead',
    businessName: 'RescuePlate Admin Central',
    phone: '+1 (555) 000-1111',
    location: 'RescuePlate HQ, New York, NY'
  };

  const mockDonations = [
    {
      id: 'don-201',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      foodTitle: 'Artisan Baguettes & Rye Bread',
      category: 'Bakery & Pastries',
      totalQuantity: 30,
      remainingQuantity: 15,
      unit: 'loaves',
      expiryHours: 6,
      expiryTime: new Date(Date.now() + 6 * 3600000).toISOString(),
      location: '450 5th Ave, New York, NY',
      dietaryTags: 'Vegetarian, Fresh',
      status: 'Available',
      notes: 'Freshly baked this morning, ready for collection.'
    },
    {
      id: 'don-202',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      foodTitle: 'Warm Croissants & Danish',
      category: 'Bakery & Pastries',
      totalQuantity: 20,
      remainingQuantity: 20,
      unit: 'pieces',
      expiryHours: 4,
      expiryTime: new Date(Date.now() + 4 * 3600000).toISOString(),
      location: '450 5th Ave, New York, NY',
      dietaryTags: 'Vegetarian',
      status: 'Available',
      notes: 'Morning pastries'
    }
  ];

  const mockPortionRequests = [
    {
      id: 'req-301',
      donationId: 'don-201',
      donationTitle: 'Artisan Baguettes & Rye Bread',
      organizationId: 'usr-org-1',
      organizationName: 'Community Hope Shelter & Kitchen',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      requestedQuantity: 15,
      unit: 'loaves',
      status: 'PENDING',
      message: 'Urgently needed for tonight dinner service at shelter.',
      createdAt: new Date().toISOString()
    },
    {
      id: 'req-302',
      donationId: 'don-201',
      donationTitle: 'Artisan Baguettes & Rye Bread',
      organizationId: 'usr-org-2',
      organizationName: 'St. Mary Youth Center',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      requestedQuantity: 10,
      unit: 'loaves',
      status: 'PENDING',
      message: 'For youth weekend meal package distribution.',
      createdAt: new Date().toISOString()
    }
  ];

  const mockDeliveryArranged = {
    id: 'del-401',
    requestId: 'req-301',
    donorId: 'usr-donor-1',
    organizationId: 'usr-org-1',
    pickupAddress: '450 5th Ave, New York, NY',
    deliveryAddress: '350 W 42nd St, New York, NY',
    contactName: 'David Martinez',
    contactPhone: '+1 (555) 345-6789',
    scheduledCollectionTime: new Date(Date.now() + 86400000).toISOString(),
    status: 'CollectionArranged',
    notes: 'Please buzz side delivery dock',
    collectedAt: null,
    receivedAt: null,
    completedAt: null
  };

  const mockNeedRequests = [
    {
      id: 'need-501',
      organizationId: 'usr-org-1',
      organizationName: 'Community Hope Shelter & Kitchen',
      title: 'Urgently Requesting Fresh Produce Crates',
      category: 'Fresh Produce',
      quantityNeeded: 40,
      remainingNeeded: 25,
      unit: 'crates',
      urgencyLevel: 'High',
      location: '350 W 42nd St, New York, NY',
      description: 'Needed for evening hot meal feeding 250 residents.',
      status: 'OPEN',
      neededBy: new Date(Date.now() + 12 * 3600000).toISOString(),
      createdAt: new Date().toISOString(),
      totalOffersCount: 2
    }
  ];

  const mockNeedOffers = [
    {
      id: 'offer-601',
      needRequestId: 'need-501',
      orgFoodNeedRequestTitle: 'Urgently Requesting Fresh Produce Crates',
      donorId: 'usr-donor-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      foodType: 'Fresh Apples & Oranges Crate',
      offeredQuantity: 15,
      unit: 'crates',
      notes: 'High quality surplus farm fresh crates.',
      status: 'PENDING',
      createdAt: new Date().toISOString()
    },
    {
      id: 'offer-602',
      needRequestId: 'need-501',
      orgFoodNeedRequestTitle: 'Urgently Requesting Fresh Produce Crates',
      donorId: 'usr-donor-2',
      donorName: 'Green Valley Organic Market',
      foodType: 'Organic Carrots and Potatoes',
      offeredQuantity: 10,
      unit: 'crates',
      notes: 'Root vegetables freshly harvested.',
      status: 'PENDING',
      createdAt: new Date().toISOString()
    }
  ];

  beforeEach(() => {
    cy.viewport(1280, 800);

    // Auto-accept any browser confirm/alert dialogs (e.g. window.confirm)
    cy.on('window:confirm', () => true);
    cy.on('window:alert', () => true);

    // Global baseline API intercepts
    cy.intercept('GET', '**/api/notifications*', { statusCode: 200, body: { success: true, data: [] } });
    cy.intercept('GET', '**/api/deliveries/notifications*', { statusCode: 200, body: { success: true, data: [] } });
  });

  // =========================================================================
  // SUITE 1: Surplus Food Donation & Delivery Lifecycle (Donor <-> Charity)
  // =========================================================================
  describe('Suite 1: Surplus Food Donation & Delivery Lifecycle', () => {

    it('01. Donor views requests received and accepts pending charity portion request', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: mockPortionRequests }
      }).as('getReceivedRequests');

      cy.intercept('GET', '**/api/deliveries/request/*', {
        statusCode: 404,
        body: { success: false, message: 'Delivery arrangement not found' }
      });

      const acceptedReq = { ...mockPortionRequests[0], status: 'ACCEPTED' };
      cy.intercept('POST', '**/api/requests/req-301/accept', {
        statusCode: 200,
        body: { success: true, message: 'Donation request accepted successfully!', data: acceptedReq }
      }).as('acceptRequest');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');

      // Verify listing grouping and requests
      cy.contains('Artisan Baguettes & Rye Bread').should('be.visible');
      cy.contains('Community Hope Shelter & Kitchen').should('be.visible');
      cy.contains('Pending Review').should('be.visible');

      // Click Accept Request button on req-301
      cy.contains('button', 'Accept Request').first().click({ force: true });
      cy.wait('@acceptRequest');

      // Success notification is displayed
      cy.contains('Donation request accepted successfully!').should('be.visible');
      cy.screenshot('01-donor-accepts-request');
    });

    it('02. Delivery arrangement workflow from accepted surplus request', () => {
      cy.loginAsCharity();

      const acceptedRequests = [{ ...mockPortionRequests[0], status: 'ACCEPTED' }];

      cy.intercept('GET', '**/api/requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: acceptedRequests }
      }).as('getMyRequests');

      let currentDelivery = null;
      cy.intercept('GET', '**/api/deliveries/request/req-301*', (req) => {
        if (currentDelivery) {
          req.reply({ statusCode: 200, body: { success: true, data: currentDelivery } });
        } else {
          req.reply({ statusCode: 404, body: { success: false, message: 'Delivery arrangement not found' } });
        }
      }).as('getDeliveryReq');

      cy.intercept('POST', '**/api/deliveries/arrange', (req) => {
        currentDelivery = { ...mockDeliveryArranged };
        req.reply({
          statusCode: 201,
          body: {
            success: true,
            message: 'Delivery collection arranged successfully!',
            data: mockDeliveryArranged
          }
        });
      }).as('arrangeDelivery');

      cy.visit('/requests');
      cy.wait('@getMyRequests');
      cy.wait('@getDeliveryReq');

      // Verify Arrange Collection CTA appears for accepted request
      cy.contains('button', 'Arrange Collection').should('be.visible').click({ force: true });

      // Verify delivery arrangement modal
      cy.contains('Arrange Donation Collection').should('be.visible');

      // Fill pickup and contact fields in arrangement modal
      cy.get('.form-input').filter('input[type="text"]').first().clear().type('450 5th Ave, Bakery Loading Dock');
      cy.get('input[type="tel"]').clear().type('+15553456789');

      // Set guaranteed future date & time (tomorrow afternoon)
      const futureDate = new Date(Date.now() + 24 * 3600 * 1000).toISOString().slice(0, 10) + 'T14:00';
      cy.get('.modal-card input[type="datetime-local"]').clear().type(futureDate);

      // Submit arrangement
      cy.contains('button', 'Confirm Arrangement').click({ force: true });
      cy.wait('@arrangeDelivery');
      cy.wait('@getDeliveryReq');

      // Verify success status
      cy.contains('Collection Arranged').should('be.visible');
      cy.screenshot('02-delivery-arranged-modal');
    });

    it('03. Donor marks delivery as collected / delivered', () => {
      cy.loginAsDonor();

      const acceptedRequests = [{ ...mockPortionRequests[0], status: 'ACCEPTED' }];

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: acceptedRequests }
      }).as('getReceivedRequests');

      cy.intercept('GET', '**/api/deliveries/request/req-301*', {
        statusCode: 200,
        body: { success: true, data: mockDeliveryArranged }
      }).as('getDeliveryArranged');

      const collectedDelivery = {
        ...mockDeliveryArranged,
        status: 'Collected',
        collectedAt: new Date().toISOString()
      };

      cy.intercept('POST', '**/api/deliveries/del-401/collect', {
        statusCode: 200,
        body: { success: true, message: 'Collection recorded successfully!', data: collectedDelivery }
      }).as('recordCollection');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');
      cy.wait('@getDeliveryArranged');

      // Verify Mark as Delivered button is present for donor
      cy.contains('button', 'Mark as Delivered').should('be.visible').click({ force: true });

      // Modal prompt for notes
      cy.get('textarea').type('Handed over 15 loaves in clean insulated boxes.');
      cy.get('.modal-card').contains('button', 'Mark as Delivered').click({ force: true });
      cy.wait('@recordCollection');

      // Verify status transitions to Delivered
      cy.contains('Delivered').should('be.visible');
      cy.screenshot('03-donor-delivery-marked-delivered');
    });

    it('04. Charity confirms receipt and enters food condition review', () => {
      cy.loginAsCharity();

      const acceptedRequests = [{ ...mockPortionRequests[0], status: 'ACCEPTED' }];
      let deliveryState = {
        ...mockDeliveryArranged,
        status: 'Collected',
        collectedAt: new Date().toISOString()
      };

      cy.intercept('GET', '**/api/requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: acceptedRequests }
      }).as('getMyRequests');

      cy.intercept('GET', '**/api/deliveries/request/req-301*', (req) => {
        req.reply({ statusCode: 200, body: { success: true, data: deliveryState } });
      }).as('getDeliveryState');

      cy.intercept('POST', '**/api/deliveries/del-401/receive', (req) => {
        deliveryState = {
          ...deliveryState,
          status: 'Received',
          receivedAt: new Date().toISOString()
        };
        req.reply({ statusCode: 200, body: { success: true, message: 'Receipt confirmed successfully!', data: deliveryState } });
      }).as('confirmReceipt');

      cy.visit('/requests');
      cy.wait('@getMyRequests');
      cy.wait('@getDeliveryState');

      // Charity sees Confirm Receipt button
      cy.contains('button', 'Confirm Receipt').should('be.visible').click({ force: true });

      // Enter receipt verification notes
      cy.get('textarea').type('Received all 15 loaves in excellent warm condition.');
      cy.get('.modal-card').contains('button', 'Confirm Receipt').click({ force: true });
      cy.wait('@confirmReceipt');
      cy.wait('@getDeliveryState');

      // Verify status transitions to Received
      cy.contains('Received').should('be.visible');
      cy.screenshot('04-charity-receipt-confirmed');
    });

    it('05. Complete donation and verify completed status sync and timeline history', () => {
      cy.loginAsDonor();

      const acceptedRequests = [{ ...mockPortionRequests[0], status: 'ACCEPTED' }];
      let deliveryState = {
        ...mockDeliveryArranged,
        status: 'Received',
        collectedAt: new Date(Date.now() - 3600000).toISOString(),
        receivedAt: new Date().toISOString()
      };

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: acceptedRequests }
      }).as('getReceivedRequests');

      cy.intercept('GET', '**/api/deliveries/request/req-301*', (req) => {
        req.reply({ statusCode: 200, body: { success: true, data: deliveryState } });
      }).as('getDeliveryState');

      const completedDelivery = {
        ...deliveryState,
        status: 'Completed',
        completedAt: new Date().toISOString(),
        statusHistory: [
          {
            id: 1,
            previousStatus: 'Created',
            newStatus: 'CollectionArranged',
            timestamp: new Date(Date.now() - 7200000).toISOString(),
            changedByUserId: 'usr-org-1',
            changedByRole: 'Organization'
          },
          {
            id: 2,
            previousStatus: 'CollectionArranged',
            newStatus: 'Collected',
            timestamp: new Date(Date.now() - 3600000).toISOString(),
            changedByUserId: 'usr-donor-1',
            changedByRole: 'Donor'
          },
          {
            id: 3,
            previousStatus: 'Collected',
            newStatus: 'Received',
            timestamp: new Date(Date.now() - 1800000).toISOString(),
            changedByUserId: 'usr-org-1',
            changedByRole: 'Organization'
          },
          {
            id: 4,
            previousStatus: 'Received',
            newStatus: 'Completed',
            timestamp: new Date().toISOString(),
            changedByUserId: 'usr-donor-1',
            changedByRole: 'Donor'
          }
        ]
      };

      cy.intercept('POST', '**/api/deliveries/del-401/complete', (req) => {
        deliveryState = completedDelivery;
        req.reply({ statusCode: 200, body: { success: true, message: 'Donation workflow marked completed!', data: completedDelivery } });
      }).as('completeDonation');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');
      cy.wait('@getDeliveryState');

      // Click Complete Donation
      cy.contains('button', 'Complete Donation').should('be.visible').click({ force: true });
      cy.get('textarea').type('Donation distributed successfully to community members.');
      cy.get('.modal-card').contains('button', 'Complete Donation').click({ force: true });
      cy.wait('@completeDonation');
      cy.wait('@getDeliveryState');

      // Status badge is Completed
      cy.contains('Completed').should('be.visible');

      // Open Track Delivery Timeline
      cy.contains('button', 'Track Delivery').click({ force: true });
      cy.contains(/Delivery Tracking/i).should('be.visible');
      cy.contains('Status Transition Log').should('be.visible');
      cy.contains('Arranged').should('be.visible');
      cy.contains('Delivered').should('be.visible');
      cy.contains('Received').should('be.visible');
      cy.contains('Completed').should('be.visible');
      cy.screenshot('05-completed-delivery-tracking-timeline');
    });

  });

  // =========================================================================
  // SUITE 2: Organization Food Need & Offer Delivery Lifecycle
  // =========================================================================
  describe('Suite 2: Organization Food Needs & Offer Delivery Lifecycle', () => {

    it('06. Charity Food Needs page defaults to Open Requests and displays open charity demands', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests?status=OPEN*', {
        statusCode: 200,
        body: { success: true, data: mockNeedRequests }
      }).as('getMyOpenNeeds');

      cy.visit('/food-needs');
      cy.wait('@getMyOpenNeeds');

      // Verify page title and header
      cy.contains('Charity Food Needs & Offers').should('be.visible');

      // Verify default status filter is Open Needs
      cy.contains('Open Needs').should('be.visible');
      cy.contains('Urgently Requesting Fresh Produce Crates').should('be.visible');
      cy.contains('40 crates').should('be.visible');
      cy.screenshot('06-charity-open-food-needs');
    });

    it('07. Donor submits food offer and verifies card in "My Submitted Food Offers" tab', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/need-requests/active*', {
        statusCode: 200,
        body: { success: true, data: mockNeedRequests }
      }).as('getActiveNeeds');

      cy.intercept('POST', '**/api/need-requests/need-501/offers', {
        statusCode: 201,
        body: { success: true, message: 'Food offer submitted successfully!', data: mockNeedOffers[0] }
      }).as('submitOffer');

      cy.intercept('GET', '**/api/offers/my-offers*', {
        statusCode: 200,
        body: { success: true, data: [mockNeedOffers[0]] }
      }).as('getMyOffers');

      cy.visit('/food-needs');
      cy.wait('@getActiveNeeds');

      // Click Offer Food to Charity on active need card
      cy.contains('button', 'Offer Food to Charity').first().click({ force: true });

      // Modal appears
      cy.contains('Offer Food to Charity').should('be.visible');
      cy.get('input.form-input').filter('[type="text"]').type('Fresh Apples & Oranges Crate');
      cy.get('input.form-input').filter('[type="number"]').focus().type('{selectall}10');
      cy.get('textarea.form-input').type('High quality surplus farm fresh crates.');

      cy.contains('button', 'Submit Food Offer').click({ force: true });
      cy.wait('@submitOffer');

      // Switch to "My Submitted Food Offers" tab
      cy.contains('button', 'My Submitted Food Offers').click({ force: true });
      cy.wait('@getMyOffers');

      // Verify offer card is listed with status chip
      cy.contains('Fresh Apples & Oranges Crate').should('be.visible');
      cy.contains('Pending Review').should('be.visible');
      cy.screenshot('07-donor-my-submitted-food-offers');
    });

    it('08. Charity views Offers Received modal, accepts donor offer, and arranges delivery', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: mockNeedRequests }
      }).as('getMyNeeds');

      let currentOffers = [...mockNeedOffers];
      cy.intercept('GET', '**/api/need-requests/need-501/offers', (req) => {
        req.reply({ statusCode: 200, body: { success: true, data: currentOffers } });
      }).as('getOffersForNeed');

      const acceptedOffer = { ...mockNeedOffers[0], status: 'ACCEPTED' };
      cy.intercept('POST', '**/api/offers/offer-601/accept', (req) => {
        currentOffers = [{ ...acceptedOffer }, mockNeedOffers[1]];
        req.reply({ statusCode: 200, body: { success: true, message: 'Offer accepted successfully!', data: acceptedOffer } });
      }).as('acceptOffer');

      let offerDeliveryState = null;
      cy.intercept('GET', '**/api/deliveries/request/offer-601*', (req) => {
        if (offerDeliveryState) {
          req.reply({ statusCode: 200, body: { success: true, data: offerDeliveryState } });
        } else {
          req.reply({ statusCode: 404, body: { success: false, message: 'Delivery arrangement not found' } });
        }
      }).as('getOfferDeliveryTracking');

      cy.intercept('POST', '**/api/deliveries/arrange', (req) => {
        offerDeliveryState = {
          ...mockDeliveryArranged,
          id: 'del-402',
          requestId: 'offer-601'
        };
        req.reply({
          statusCode: 201,
          body: {
            success: true,
            message: 'Delivery collection arranged successfully!',
            data: offerDeliveryState
          }
        });
      }).as('arrangeOfferDelivery');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      // Open Offers Received modal
      cy.contains('button', /Offers \(\d+\)/).click({ force: true });
      cy.wait('@getOffersForNeed');

      // Accept the first offer
      cy.contains('Fresh Apples & Oranges Crate').should('be.visible');
      cy.contains('button', 'Accept Offer').first().click({ force: true });
      cy.wait('@acceptOffer');
      cy.wait('@getOffersForNeed');

      // Offer card updates to Accepted
      cy.contains('Accepted').should('be.visible');

      // Click Arrange Delivery directly inside the Offers modal
      cy.contains('button', 'Arrange Delivery').click({ force: true });
      cy.contains('Arrange Food Delivery / Collection').should('be.visible');

      cy.get('.form-input').filter('[type="text"]').first().clear().type('450 5th Ave, Grand Horizon Bakery');
      cy.get('.form-input').filter('[type="text"]').eq(1).clear().type('Father David');
      cy.get('.form-input').filter('[type="tel"]').clear().type('+15553456789');

      // Set guaranteed future date & time (tomorrow afternoon)
      const futureDate = new Date(Date.now() + 24 * 3600 * 1000).toISOString().slice(0, 10) + 'T14:00';
      cy.get('input[type="datetime-local"]').clear().type(futureDate);

      cy.contains('button', 'Confirm Arrangement').click({ force: true });
      cy.wait('@arrangeOfferDelivery');

      // Verify delivery arrangement badge appears on the offer card in modal
      cy.contains('Collection Arranged').should('be.visible');
      cy.screenshot('08-charity-accepts-offer-arranged');
    });

    it('09. Donor accesses delivery workflow directly from "My Submitted Food Offers" tab', () => {
      cy.loginAsDonor();

      const acceptedOffer = { ...mockNeedOffers[0], status: 'ACCEPTED' };
      const offerDelivery = {
        id: 'del-402',
        requestId: 'offer-601',
        donorId: 'usr-donor-1',
        organizationId: 'usr-org-1',
        pickupAddress: '450 5th Ave, Grand Horizon Bakery',
        deliveryAddress: '350 W 42nd St, New York, NY',
        contactName: 'Sarah Jenkins',
        contactPhone: '+1 (555) 234-5678',
        scheduledCollectionTime: new Date(Date.now() + 7200000).toISOString(),
        status: 'CollectionArranged'
      };

      cy.intercept('GET', '**/api/offers/my-offers*', {
        statusCode: 200,
        body: { success: true, data: [acceptedOffer] }
      }).as('getMyOffers');

      cy.intercept('GET', '**/api/deliveries/request/offer-601*', {
        statusCode: 200,
        body: { success: true, data: offerDelivery }
      }).as('getOfferDelivery');

      cy.visit('/food-needs');

      // Switch to My Submitted Food Offers tab
      cy.contains('button', 'My Submitted Food Offers').click({ force: true });
      cy.wait('@getMyOffers');
      cy.wait('@getOfferDelivery');

      // Accepted offer displays Track Delivery button
      cy.contains('button', 'Track Delivery').should('be.visible').click({ force: true });

      // Delivery tracking modal opens
      cy.contains('Collection Arranged').should('be.visible');
      cy.contains('450 5th Ave, Grand Horizon Bakery').should('be.visible');
      cy.screenshot('09-donor-tracks-offer-delivery');
    });

  });

  // =========================================================================
  // SUITE 3: Bulk Rejection Workflows
  // =========================================================================
  describe('Suite 3: Bulk Rejection Workflows', () => {

    it('10. Donor bulk rejects all pending requests for a surplus donation listing with custom reason', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/requests/received*', {
        statusCode: 200,
        body: { success: true, data: mockPortionRequests }
      }).as('getReceivedRequests');

      cy.intercept('GET', '**/api/deliveries/request/*', {
        statusCode: 404,
        body: { success: false, message: 'Delivery arrangement not found' }
      });

      cy.intercept('POST', '**/api/requests/donations/don-201/reject-all-pending', {
        statusCode: 200,
        body: {
          success: true,
          message: 'All 2 pending requests have been declined.',
          count: 2
        }
      }).as('bulkRejectDonationRequests');

      cy.visit('/requests');
      cy.wait('@getReceivedRequests');

      // Verify the listing-level banner container contains Reject All Pending
      cy.contains('button', 'Reject All Pending').should('be.visible').click({ force: true });

      // Bulk rejection confirmation dialog
      cy.contains('Reject All Pending Requests').should('be.visible');
      cy.get('textarea').clear().type('All surplus food portions have been fully allocated.');

      // Confirm bulk rejection
      cy.contains('button', 'Confirm Reject All').click({ force: true });
      cy.wait('@bulkRejectDonationRequests');

      cy.contains('All 2 pending requests have been declined.').should('be.visible');
      cy.screenshot('10-donor-bulk-reject-all-pending');
    });

    it('11. Charity bulk rejects all pending offers for an organization food need with custom reason', () => {
      cy.loginAsCharity();

      cy.intercept('GET', '**/api/need-requests/my-requests*', {
        statusCode: 200,
        body: { success: true, data: mockNeedRequests }
      }).as('getMyNeeds');

      cy.intercept('GET', '**/api/need-requests/need-501/offers', {
        statusCode: 200,
        body: { success: true, data: mockNeedOffers }
      }).as('getOffersForNeed');

      cy.intercept('POST', '**/api/need-requests/need-501/offers/reject-all-pending', {
        statusCode: 200,
        body: {
          success: true,
          message: 'All 2 pending food offers were politely declined.',
          count: 2
        }
      }).as('bulkRejectOffers');

      cy.visit('/food-needs');
      cy.wait('@getMyNeeds');

      cy.contains('button', /Offers \(\d+\)/).click({ force: true });
      cy.wait('@getOffersForNeed');

      // Verify listing banner has Reject All Pending Offers
      cy.contains('button', 'Reject All Pending Offers').should('be.visible').click({ force: true });

      // Modal prompt for bulk rejection
      cy.contains('Reject All Pending Offers').should('be.visible');
      cy.get('textarea').clear().type('Requirement satisfied through direct local farm drop-off.');

      cy.contains('button', 'Confirm Reject All').click({ force: true });
      cy.wait('@bulkRejectOffers');

      cy.contains('All 2 pending food offers were politely declined.').should('be.visible');
      cy.screenshot('11-charity-bulk-reject-all-pending');
    });

  });

  // =========================================================================
  // SUITE 4: UI Navigation & Modal Ergonomics
  // =========================================================================
  describe('Suite 4: UI Navigation & Modal Ergonomics', () => {

    it('12. Donor Post Surplus page: View Details popup auto-scrolls to top', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: mockDonations }
      }).as('getMyDonations');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Scroll down the page
      cy.scrollTo('bottom');
      cy.wait(300);

      // Click Details button on a donation card
      cy.contains('button', 'Details').first().click({ force: true });

      // Verify window scrolled to top
      cy.window().its('scrollY').should('equal', 0);

      // Verify details modal is visible and contains listing information
      cy.contains('Artisan Baguettes & Rye Bread').should('be.visible');
      cy.screenshot('12-donor-view-details-modal');
      cy.contains('button', 'Close').click({ force: true });
    });

    it('13. Donor Post Surplus page: Edit Details and Cancel Listing popups auto-scroll and remain unobscured', () => {
      cy.loginAsDonor();

      cy.intercept('GET', '**/api/donations/my-donations*', {
        statusCode: 200,
        body: { success: true, data: mockDonations }
      }).as('getMyDonations');

      cy.visit('/donor-portal');
      cy.wait('@getMyDonations');

      // Scroll down
      cy.scrollTo(0, 600);
      cy.wait(300);

      // Click Edit button
      cy.contains('button', 'Edit').first().click({ force: true });

      // Auto-scrolls to top
      cy.window().its('scrollY').should('equal', 0);
      cy.contains('Edit Food Donation').should('be.visible');

      // Close edit modal via Cancel button inside edit form
      cy.contains('Modify food details').parents('[style*="position: fixed"]').find('button').contains('Cancel').click({ force: true });
      cy.contains('Edit Food Donation').should('not.exist');

      // Scroll down again and click Cancel button on the donation card
      cy.scrollTo(0, 600);
      cy.wait(300);
      cy.contains('.profile-card', 'Artisan Baguettes & Rye Bread').find('button').contains('Cancel').click({ force: true });

      // Auto-scrolls to top
      cy.window().its('scrollY').should('equal', 0);

      // Cancel modal is displayed with clear padding
      cy.contains('Cancel Surplus Food Donation?').should('be.visible');
      cy.get('select').should('contain', 'Surplus food consumed / donated locally');
      cy.screenshot('13-donor-cancel-listing-unobscured-modal');
      cy.contains('button', 'Keep Donation').click({ force: true });
    });

  });

  // =========================================================================
  // SUITE 5: Admin Portal Isolation & Security
  // =========================================================================
  describe('Suite 5: Admin Portal Isolation & Security', () => {

    it('14. Admin navigation bar is strictly isolated to administrative routes only', () => {
      cy.loginAsAdmin();

      cy.intercept('GET', '**/api/admin/users*', {
        statusCode: 200,
        body: { success: true, data: [mockDonorUser, mockCharityUser] }
      }).as('getAdminUsers');

      cy.intercept('GET', '**/api/admin/monitoring/overview*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            totalUsers: 150,
            activeDonations: 45,
            completedDeliveries: 120,
            systemHealth: 'Optimal'
          }
        }
      }).as('getMonitoring');

      cy.intercept('GET', '**/api/admin/monitoring/activity*', {
        statusCode: 200,
        body: { success: true, data: [] }
      });

      cy.visit('/admin/dashboard');

      // Verify Admin Hub navbar item is present
      cy.contains('🛡️ Admin Hub').should('be.visible');

      // Verify public browsing links are NOT present in navbar
      cy.get('.desktop-nav').within(() => {
        cy.contains('Browse Food').should('not.exist');
        cy.contains('Post Surplus').should('not.exist');
        cy.contains('Charities').should('not.exist');
        cy.contains('Donors').should('not.exist');
        cy.contains('Food Needs').should('not.exist');
      });

      // Notification bell is hidden for Admin
      cy.get('button[title="Notifications"]').should('not.exist');
      cy.screenshot('14-admin-isolated-navigation-bar');
    });

    it('15. Admin moderation dashboard and profile route protection', () => {
      cy.loginAsAdmin();

      const mockUsersList = [
        {
          id: 'usr-donor-1',
          email: 'sarah.jenkins@grandhorizon.org',
          role: 'DONOR',
          businessName: 'Grand Horizon Bakery & Cafe',
          contactName: 'Sarah Jenkins',
          location: '450 5th Ave, New York, NY',
          isActive: true,
          createdAt: new Date().toISOString()
        },
        {
          id: 'usr-org-1',
          email: 'david@communityhopeshelter.org',
          role: 'ORGANIZATION',
          businessName: 'Community Hope Shelter & Kitchen',
          contactName: 'David Martinez',
          location: '350 W 42nd St, New York, NY',
          isActive: false,
          createdAt: new Date().toISOString()
        }
      ];

      cy.intercept('GET', '**/api/admin/users*', {
        statusCode: 200,
        body: { success: true, data: mockUsersList }
      }).as('getAdminUsers');

      cy.intercept('GET', '**/api/admin/monitoring/overview*', {
        statusCode: 200,
        body: { success: true, data: { totalUsers: 2 } }
      });

      cy.intercept('GET', '**/api/admin/monitoring/activity*', {
        statusCode: 200,
        body: { success: true, data: [] }
      });

      cy.intercept('PATCH', '**/api/admin/users/usr-donor-1/status', {
        statusCode: 200,
        body: { success: true, message: 'User status updated successfully!' }
      }).as('toggleStatus');

      cy.visit('/admin/dashboard');
      cy.wait('@getAdminUsers');

      // Verify user moderation table renders
      cy.contains('Grand Horizon Bakery & Cafe').should('be.visible');
      cy.contains('Community Hope Shelter & Kitchen').should('be.visible');
      cy.screenshot('15-admin-moderation-dashboard');

      // Attempt navigating to /profile as Admin -> verifies auto-redirect to /admin/dashboard
      cy.visit('/profile');
      cy.url().should('include', '/admin/dashboard');
    });

  });

});
