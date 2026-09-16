describe('RP-22 to RP-26: Donor Dashboard', () => {
  const generateRandomEmail = () => `testdonor_${Date.now()}_${Math.floor(Math.random() * 1000)}@test.com`;

  describe('Create and View Donations (RP-22, RP-23)', () => {
    let currentUserEmail;

    beforeEach(() => {
      currentUserEmail = generateRandomEmail();
      cy.registerViaApi({
        email: currentUserEmail,
        password: 'Password123!',
        role: 'DONOR',
        businessOrOrgName: 'Dashboard Bakery',
        location: '456 Donor St'
      }).then(() => {
        cy.loginViaApi(currentUserEmail, 'Password123!');
      });
    });

    it('should successfully create a new donation and view it in the dashboard', () => {
      cy.visit('/donor-portal');
      
      // Open Create Modal
      cy.get('button').contains('Post Food Donation').click();
      cy.contains('h3', 'Post Surplus Food Donation').should('be.visible');

      // Fill Form
      cy.get('input[name="foodTitle"]').type('Fresh Cinnamon Rolls');
      cy.get('select[name="category"]').select('Bakery & Pastries');
      cy.get('input[name="totalQuantity"]').clear().type('12');
      cy.get('select[name="unit"]').select('portions');
      cy.get('select[name="expiryHours"]').select('8');
      
      // Submit Form
      cy.contains('button', 'Publish Donation').click();

      // Check success and appearance in grid
      cy.contains('Surplus food donation posted successfully!', { timeout: 20000 }).should('be.visible');
      cy.screenshot('donor-dashboard-donation-creation-success');

      // Verify item appears in grid
      cy.get('.profile-card').contains('Fresh Cinnamon Rolls').should('be.visible');
    });

    it('should show validation errors when creating with invalid data', () => {
      cy.visit('/donor-portal');
      cy.get('button').contains('Post Food Donation').click();
      
      // Bypass HTML5 validation to test React validation
      cy.get('form').invoke('attr', 'novalidate', 'novalidate');
      
      cy.get('input[name="foodTitle"]').type('Invalid Donation');
      cy.get('input[name="totalQuantity"]').clear().type('-5');
      
      cy.contains('button', 'Publish Donation').click();
      
      cy.contains('must be a positive number').should('be.visible');
    });
  });

  describe('Edit and Cancel Donations (RP-24, RP-26)', () => {
    let currentUserEmail;

    beforeEach(() => {
      currentUserEmail = generateRandomEmail();
      cy.registerViaApi({
        email: currentUserEmail,
        password: 'Password123!',
        role: 'DONOR',
        businessOrOrgName: 'Edit Bakery',
        location: '789 Edit St'
      }).then((res) => {
        cy.loginViaApi(currentUserEmail, 'Password123!').then(() => {
          // Create a donation via API for editing/cancelling
          cy.createDonationViaApi({
            foodTitle: 'Sourdough Bread',
            category: 'Bakery & Pastries',
            totalQuantity: 5,
            unit: 'portions',
            expiryHours: 24,
            collectionMode: 'Organization Pickup',
            location: '789 Edit St',
            notes: 'Freshly baked',
            dietaryTags: 'Vegan'
          });
        });
      });
    });

    it('should successfully edit an existing donation', () => {
      cy.visit('/donor-portal');
      
      // Ensure it's loaded
      cy.get('.profile-card').contains('Sourdough Bread').should('be.visible');
      
      // Click Edit
      cy.get('.profile-card').contains('Sourdough Bread').parents('.profile-card').find('button').contains('Edit').click();
      
      cy.contains('Edit Food Donation').should('be.visible');
      
      // Edit Title
      cy.get('input[name="foodTitle"]').clear().type('Sourdough Bread - Discounted');
      cy.contains('button', 'Save Changes').click();
      
      cy.contains('updated successfully', { timeout: 20000 }).should('be.visible');
      cy.screenshot('donor-dashboard-donation-edit-success');
      
      cy.get('.profile-card').contains('Sourdough Bread - Discounted').should('be.visible');
    });

    it('should successfully cancel an existing donation', () => {
      cy.visit('/donor-portal');
      
      cy.get('.profile-card').contains('Sourdough Bread').should('be.visible');
      
      // Click Cancel
      cy.get('.profile-card').contains('Sourdough Bread').parents('.profile-card').find('button').contains('Cancel').click();
      
      cy.contains('Donation ID #').should('be.visible');
      
      // Select reason
      cy.get('select').select('Items spoiled / freshness threshold lapsed');
      
      cy.contains('button', 'Yes, Cancel Listing').click();
      
      cy.contains('cancelled successfully', { timeout: 20000 }).should('be.visible');
      cy.screenshot('donor-dashboard-donation-cancel-success');
      
      // Verify Status update visually
      cy.get('.profile-card').contains('Sourdough Bread').parents('.profile-card').contains('Cancelled').should('be.visible');
    });
  });
});
