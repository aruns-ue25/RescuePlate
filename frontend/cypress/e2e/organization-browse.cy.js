describe('RP-27 & RP-29: Organization Browse and Details', () => {
  const generateRandomEmail = () => `testdonor_${Date.now()}_${Math.floor(Math.random() * 1000)}@test.com`;

  beforeEach(() => {
    // We need some active donations to browse. We'll create a donor, log in as donor, create a donation, then log out (or just browse unauthenticated since route is public).
    const donorEmail = generateRandomEmail();
    
    cy.registerViaApi({
      email: donorEmail,
      password: 'Password123!',
      role: 'DONOR',
      businessOrOrgName: 'Browse Bakery',
      location: '123 Browse St'
    }).then(() => {
      cy.loginViaApi(donorEmail, 'Password123!').then(() => {
        cy.createDonationViaApi({
          foodTitle: 'Unique Test Browse Meal',
          category: 'Cooked Meals',
          totalQuantity: 20,
          unit: 'portions',
          expiryHours: 12,
          collectionMode: 'Organization Pickup',
          location: '123 Browse St',
          notes: 'Special preparation needed',
          dietaryTags: 'Halal'
        });
      });
    });
  });

  it('should list available donations and allow searching/filtering', () => {
    cy.visit('/browse-food');
    
    // Check if item exists in grid
    cy.contains('Unique Test Browse Meal').should('be.visible');
    
    // Test Search
    cy.get('input[placeholder="Search available food or donor..."]').type('Unique Test');
    cy.get('button').contains('Search').click();
    
    cy.contains('Unique Test Browse Meal').should('be.visible');
    cy.screenshot('org-browse-search-results');
    
    // Test Filter
    cy.get('button').contains('Bakery').click(); // Different category
    cy.contains('No Available Surplus Food').should('be.visible');
    
    cy.get('button').contains('Cooked Meals').click(); // Correct category
    cy.contains('Unique Test Browse Meal').should('be.visible');
  });

  it('should open donation details modal (RP-29)', () => {
    cy.visit('/browse-food');
    
    // Find the specific donation and click Details
    cy.contains('Unique Test Browse Meal')
      .parents('div')
      .first() // Get the card container
      .parent() // Depending on DOM, step up
      .find('button').contains('Details')
      .click({ force: true });
      
    // Verify modal is open and shows correct info
    cy.contains('Preparation Notes & Instructions').should('be.visible');
    cy.contains('Special preparation needed').should('be.visible');
    cy.screenshot('org-browse-donation-details');
    
    // Close modal
    cy.get('button[title="Close details"]').click();
    cy.contains('Preparation Notes & Instructions').should('not.exist');
  });
});
