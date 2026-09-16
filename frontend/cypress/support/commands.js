// Custom command to login via API and set localStorage
Cypress.Commands.add('loginViaApi', (email, password) => {
  return cy.request({
    method: 'POST',
    url: 'http://localhost:5000/api/auth/login',
    body: { email, password },
    failOnStatusCode: false
  }).then((response) => {
    if (response.status === 200 && response.body.success) {
      window.localStorage.setItem('rescueplate_token', response.body.data.token);
      window.localStorage.setItem('rescueplate_user', JSON.stringify({
        userId: response.body.data.userId,
        email: response.body.data.email,
        role: response.body.data.role,
        name: response.body.data.name,
        businessName: response.body.data.businessName
      }));
    }
    return response;
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

Cypress.Commands.add('createDonationViaApi', (donationData) => {
  const token = window.localStorage.getItem('rescueplate_token');
  return cy.request({
    method: 'POST',
    url: 'http://localhost:5001/api/donations',
    headers: { Authorization: `Bearer ${token}` },
    body: donationData,
    failOnStatusCode: false
  });
});