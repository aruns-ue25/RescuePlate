describe('RescuePlate Complete Web Application E2E Test Suite (Clean & Pristine Screenshots)', () => {

  const mockDonations = [
    {
      id: 'don-101',
      donorId: 'usr-1',
      donorName: 'Grand Horizon Bakery & Cafe',
      foodTitle: 'Fresh Artisan Sourdough & Pastries',
      category: 'Bakery',
      remainingQuantity: 25,
      totalQuantity: 30,
      unit: 'loaves',
      expiryTime: '2026-10-15T20:00:00Z',
      location: '450 5th Ave, New York, NY',
      dietaryTags: 'Vegetarian, Freshly Baked Today',
      status: 'Available'
    },
    {
      id: 'don-102',
      donorId: 'usr-2',
      donorName: 'City Central Hotel & Catering',
      foodTitle: 'Prepared Gourmet Lunch Trays',
      category: 'Cooked Meals',
      remainingQuantity: 40,
      totalQuantity: 50,
      unit: 'portions',
      expiryTime: '2026-10-15T22:00:00Z',
      location: '120 Broadway, New York, NY',
      dietaryTags: 'Halal, Contains Dairy',
      status: 'Available'
    },
    {
      id: 'don-103',
      donorId: 'usr-3',
      donorName: 'Green Valley Organic Market',
      foodTitle: 'Assorted Organic Produce Crates',
      category: 'Produce',
      remainingQuantity: 15,
      totalQuantity: 15,
      unit: 'crates',
      expiryTime: '2026-10-16T18:00:00Z',
      location: '78 Farmers Way, Brooklyn, NY',
      dietaryTags: 'Vegan, Fresh Farm Produce',
      status: 'Available'
    }
  ];

  const mockDonors = [
    {
      id: 'usr-1',
      businessOrOrgName: 'Grand Horizon Bakery & Cafe',
      donorType: 'Bakery / Cafe',
      location: '450 5th Ave, New York, NY',
      contactPhone: '+1 (555) 234-5678',
      totalDonationsCount: 42,
      status: 'Active',
      description: 'Daily provider of surplus sourdough bread, baguettes, and fresh pastries for local community shelters.'
    },
    {
      id: 'usr-2',
      businessOrOrgName: 'City Central Hotel & Catering',
      donorType: 'Hotel / Catering',
      location: '120 Broadway, New York, NY',
      contactPhone: '+1 (555) 987-6543',
      totalDonationsCount: 89,
      status: 'Active',
      description: 'Providing high quality packaged banquet meals and gourmet items to verified non-profit partners.'
    }
  ];

  const mockOrganizations = [
    {
      id: 'org-1',
      businessOrOrgName: 'Community Hope Shelter & Kitchen',
      organizationType: 'Food Bank / Shelter',
      location: '350 W 42nd St, New York, NY',
      contactPhone: '+1 (555) 345-6789',
      requestedItemsCount: 18,
      status: 'Verified',
      description: 'Serving over 300 nutritious warm meals daily to families and individuals in need across Midtown.'
    },
    {
      id: 'org-2',
      businessOrOrgName: 'St. Mary Youth & Family Center',
      organizationType: 'Youth Center',
      location: '88 Park Ave, Brooklyn, NY',
      contactPhone: '+1 (555) 654-3210',
      requestedItemsCount: 12,
      status: 'Verified',
      description: 'Providing after-school snack packages and weekend meal assistance for underprivileged children.'
    }
  ];

  const mockNeedRequests = [
    {
      id: 'need-1',
      organizationName: 'Community Hope Shelter & Kitchen',
      title: 'Urgently Requesting Fresh Vegetables & Rice',
      category: 'Produce',
      quantityNeeded: 50,
      unit: 'kg',
      urgencyLevel: 'High',
      location: '350 W 42nd St, New York, NY',
      description: 'Preparing evening dinner meals for 250 shelter residents tonight.'
    }
  ];

  const mockUserProfile = {
    userId: 'usr-1',
    email: 'qa_tester@rescueplate.org',
    name: 'Sarah Jenkins',
    businessName: 'Grand Horizon Bakery & Cafe',
    role: 'DONOR',
    phone: '+1 (555) 234-5678',
    location: '450 5th Ave, New York, NY',
    bio: 'Dedicated to ending local food waste through daily surplus food redistribution.'
  };

  beforeEach(() => {
    // Set viewport for clean desktop layout
    cy.viewport(1280, 720);

    // Set authenticated session in localStorage
    cy.setMockAuthSession();

    // Intercept backend API requests with realistic mock data to ensure clean UI without error popups
    cy.intercept('GET', '**/api/donations*', { statusCode: 200, body: { success: true, data: mockDonations } }).as('getDonations');
    cy.intercept('GET', '**/api/donations/donors*', { statusCode: 200, body: { success: true, data: mockDonors } }).as('getDonors');
    cy.intercept('GET', '**/api/donations/organizations*', { statusCode: 200, body: { success: true, data: mockOrganizations } }).as('getOrganizations');
    cy.intercept('GET', '**/api/need-requests/active*', { statusCode: 200, body: { success: true, data: mockNeedRequests } }).as('getNeeds');
    cy.intercept('GET', '**/api/profile/me*', { statusCode: 200, body: { success: true, data: mockUserProfile } }).as('getProfile');
    cy.intercept('GET', '**/api/donations/my-donations*', { statusCode: 200, body: { success: true, data: mockDonations } }).as('getMyDonations');
    cy.intercept('GET', '**/api/requests/received*', { statusCode: 200, body: { success: true, data: [] } }).as('getReceivedRequests');
    cy.intercept('GET', '**/api/notifications*', { statusCode: 200, body: { success: true, data: [] } }).as('getNotifications');
  });

  // TEST 1: Home Page Section 1 (Hero)
  it('01. Should capture top hero section (home1)', () => {
    cy.visit('http://localhost:5173');
    cy.scrollTo('top');
    cy.wait(500);
    cy.screenshot('home1', { capture: 'viewport', overwrite: true });
  });

  // TEST 2: Home Page Section 2 (Portals & Impact Stats)
  it('02. Should capture features section (home2)', () => {
    cy.visit('http://localhost:5173');
    cy.get('.portal-cards-section').scrollIntoView();
    cy.wait(500);
    cy.screenshot('home2', { capture: 'viewport', overwrite: true });
  });

  // TEST 3: Home Page Section 3 (Features Grid)
  it('03. Should capture impact stats section (home3)', () => {
    cy.visit('http://localhost:5173');
    cy.get('.features-section').scrollIntoView();
    cy.wait(500);
    cy.screenshot('home3', { capture: 'viewport', overwrite: true });
  });

  // TEST 4: Home Page Section 4 (Footer & Categories)
  it('04. Should capture footer section (home4)', () => {
    cy.visit('http://localhost:5173');
    cy.scrollTo('bottom');
    cy.wait(500);
    cy.screenshot('home4', { capture: 'viewport', overwrite: true });
  });

  // TEST 5: Browse Surplus Food Listings Page
  it('05. Should browse surplus food listings page with clean data', () => {
    cy.visit('http://localhost:5173/browse-food');
    cy.contains('Browse Available Surplus Food').should('be.visible');
    cy.wait(600);
    cy.screenshot('browse-food', { capture: 'viewport', overwrite: true });
  });

  // TEST 6: Donor Directory Page
  it('06. Should view Donor Directory page with clean data', () => {
    cy.visit('http://localhost:5173/donors');
    cy.wait(600);
    cy.screenshot('donor-directory', { capture: 'viewport', overwrite: true });
  });

  // TEST 7: Charity Organizations Directory Page
  it('07. Should view Charity Organizations Directory with clean data', () => {
    cy.visit('http://localhost:5173/organizations');
    cy.wait(600);
    cy.screenshot('organizations-directory', { capture: 'viewport', overwrite: true });
  });

  // TEST 8: Food Needs & Offers Page
  it('08. Should view Food Needs page with clean data', () => {
    cy.visit('http://localhost:5173/food-needs');
    cy.wait(600);
    cy.screenshot('food-needs-offers', { capture: 'viewport', overwrite: true });
  });

  // TEST 9: How It Works Page
  it('09. Should verify How It Works page', () => {
    cy.visit('http://localhost:5173/how-it-works');
    cy.contains('How It Works').should('be.visible');
    cy.wait(500);
    cy.screenshot('how-it-works', { capture: 'viewport', overwrite: true });
  });

  // TEST 10: About Us Page
  it('10. Should verify About Us page', () => {
    cy.visit('http://localhost:5173/about');
    cy.wait(500);
    cy.screenshot('about-us', { capture: 'viewport', overwrite: true });
  });

  // TEST 11: Contact Us Page
  it('11. Should verify Contact Us page', () => {
    cy.visit('http://localhost:5173/contact');
    cy.wait(500);
    cy.screenshot('contact-us', { capture: 'viewport', overwrite: true });
  });

  // TEST 12: User Profile Settings Page
  it('12. Should view User Profile Settings page', () => {
    cy.visit('http://localhost:5173/profile');
    cy.wait(600);
    cy.screenshot('user-profile-page', { capture: 'viewport', overwrite: true });
  });

  // TEST 13: Donor Portal Dashboard
  it('13. Should view Donor Portal Dashboard page', () => {
    cy.visit('http://localhost:5173/donor-portal');
    cy.wait(600);
    cy.screenshot('donor-portal-page', { capture: 'viewport', overwrite: true });
  });

});
