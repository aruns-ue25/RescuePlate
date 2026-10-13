// Custom Cypress Commands for RescuePlate

Cypress.Commands.add('setMockAuthSession', () => {
  window.localStorage.setItem('rescueplate_token', 'mock_jwt_token_12345');
  window.localStorage.setItem('rescueplate_user', JSON.stringify({
    userId: 'usr-1',
    email: 'qa_tester@rescueplate.org',
    role: 'DONOR',
    name: 'Sarah Jenkins',
    businessName: 'Grand Horizon Bakery'
  }));
});

Cypress.Commands.add('loginAsDonor', (overrides = {}) => {
  const donorUser = {
    userId: 'usr-donor-1',
    id: 'usr-donor-1',
    email: 'sarah.jenkins@grandhorizon.org',
    role: 'DONOR',
    name: 'Sarah Jenkins',
    businessName: 'Grand Horizon Bakery & Cafe',
    phone: '+1 (555) 234-5678',
    location: '450 5th Ave, New York, NY',
    address: '450 5th Ave, New York, NY',
    ...overrides
  };
  window.localStorage.setItem('rescueplate_token', 'mock_jwt_donor_token_123');
  window.localStorage.setItem('rescueplate_user', JSON.stringify(donorUser));
});

Cypress.Commands.add('loginAsCharity', (overrides = {}) => {
  const orgUser = {
    userId: 'usr-org-1',
    id: 'usr-org-1',
    email: 'david@communityhopeshelter.org',
    role: 'ORGANIZATION',
    name: 'David Martinez',
    businessName: 'Community Hope Shelter & Kitchen',
    phone: '+1 (555) 345-6789',
    location: '350 W 42nd St, New York, NY',
    address: '350 W 42nd St, New York, NY',
    ...overrides
  };
  window.localStorage.setItem('rescueplate_token', 'mock_jwt_org_token_456');
  window.localStorage.setItem('rescueplate_user', JSON.stringify(orgUser));
});

Cypress.Commands.add('loginAsAdmin', (overrides = {}) => {
  const adminUser = {
    userId: 'usr-admin-1',
    id: 'usr-admin-1',
    email: 'admin@rescueplate.org',
    role: 'ADMIN',
    name: 'System Operations Lead',
    businessName: 'RescuePlate Admin Central',
    phone: '+1 (555) 000-1111',
    location: 'RescuePlate HQ, New York, NY',
    ...overrides
  };
  window.localStorage.setItem('rescueplate_token', 'mock_jwt_admin_token_789');
  window.localStorage.setItem('rescueplate_user', JSON.stringify(adminUser));
});