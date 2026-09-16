describe('RP-21, RP-30, RP-32: Public Discovery and Profiles', () => {
  const generateRandomEmail = () => `testuser_${Date.now()}_${Math.floor(Math.random() * 1000)}@test.com`;

  beforeEach(() => {
    // Seed one donor and one organization to ensure the directory has data
    const donorEmail = generateRandomEmail();
    const orgEmail = generateRandomEmail();

    cy.registerViaApi({
      email: donorEmail,
      password: 'Password123!',
      role: 'DONOR',
      businessOrOrgName: 'Discovery Donor Bakery',
      location: '123 Discovery St'
    }).then(() => {
      cy.loginViaApi(donorEmail, 'Password123!').then(() => {
        cy.createDonationViaApi({
          foodTitle: 'Discovery Bread',
          category: 'Bakery & Pastries',
          totalQuantity: 10,
          unit: 'portions',
          expiryHours: 24,
          collectionMode: 'Organization Pickup',
          location: '123 Discovery St',
          notes: 'Test'
        });
      });
    });

    cy.registerViaApi({
      email: orgEmail,
      password: 'Password123!',
      role: 'ORGANIZATION',
      businessOrOrgName: 'Discovery Food Bank',
      location: '456 Discovery St'
    });
  });

  it('should load the public home page', () => {
    cy.visit('/');
    cy.contains('RescuePlate').should('be.visible');
    // Basic check for home page elements
    cy.contains('Browse Available Food').should('be.visible');
    cy.screenshot('public-home-page');
  });

  it('should list donors and view a donor profile (RP-30, RP-21)', () => {
    cy.visit('/donors');
    
    // Directory should load
    cy.contains('h1', 'Participating Food Donors').should('be.visible');
    cy.contains('Discovery Donor Bakery').should('be.visible');
    cy.screenshot('public-donor-directory');
    
    // Click on the donor card to view profile
    cy.contains('Discovery Donor Bakery').parents('.profile-card, div').contains('button', 'View Profile & Food').click();
    
    // Verify public profile loads
    cy.contains('Read-Only Directory View').should('be.visible');
    cy.contains('Discovery Donor Bakery').should('be.visible');
    cy.screenshot('public-profile-donor');
  });

  it('should list organizations and view an org profile (RP-32, RP-21)', () => {
    cy.visit('/organizations');
    
    // Directory should load
    cy.contains('h1', 'Participating Charitable Organizations').should('be.visible');
    cy.contains('Colombo City Food Bank').should('be.visible');
    cy.screenshot('public-organization-directory');
    
    // Click on the org card to view profile
    cy.contains('Colombo City Food Bank').parents('.profile-card, div').contains('button', 'View Organization Profile').click();
    
    // Verify public profile loads
    cy.contains('Read-Only Directory View').should('be.visible');
    cy.contains('Colombo City Food Bank').should('be.visible');
    cy.screenshot('public-profile-organization');
  });
});
