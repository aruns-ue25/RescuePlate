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

Cypress.Commands.add('loginViaApi', (email, password) => {
  cy.setMockAuthSession();
  return cy.request({
    method: 'POST',
    url: 'http://localhost:5000/api/auth/login',
    body: { email, password },
    failOnStatusCode: false
  }).then((response) => {
    if (response.status === 200 && response.body.success) {
      window.localStorage.setItem('rescueplate_token', response.body.data.token);
      window.localStorage.setItem('rescueplate_user', JSON.stringify(response.body.data));
    }
  });
});

Cypress.Commands.add('registerViaApi', (user) => {
  return cy.request({
    method: 'POST',
    url: 'http://localhost:5000/api/auth/register',
    body: user,
    failOnStatusCode: false
  });
});