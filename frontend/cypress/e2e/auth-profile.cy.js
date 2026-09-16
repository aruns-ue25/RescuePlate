describe('RP-12 & RP-20: Authentication and Profile Management', () => {
  const generateRandomEmail = () => `testdonor_${Date.now()}_${Math.floor(Math.random() * 1000)}@test.com`;

  describe('Change Password (RP-12)', () => {
    let currentUserEmail;
    const oldPassword = 'OldPassword123!';
    const newPassword = 'NewStrongPassword123!';

    beforeEach(() => {
      currentUserEmail = generateRandomEmail();
      // Register a new user
      cy.registerViaApi({
        email: currentUserEmail,
        password: oldPassword,
        role: 'DONOR',
        businessOrOrgName: 'Cypress Bakery',
        location: '123 Test St'
      }).then(() => {
        // Log in
        cy.loginViaApi(currentUserEmail, oldPassword);
      });
    });

    it('should successfully change password and show success message', () => {
      cy.visit('/profile');
      
      cy.get('input[placeholder="Enter your current password"]').type(oldPassword);
      cy.get('input[placeholder="Enter strong new password"]').type(newPassword);
      cy.get('input[placeholder="Repeat new password"]').type(newPassword);
      
      cy.get('button').contains('Update Password').click();

      cy.contains('successfully').should('be.visible');
      cy.screenshot('auth-profile-change-password-success');
    });

    it('should show error for incorrect current password', () => {
      cy.visit('/profile');
      
      cy.get('input[placeholder="Enter your current password"]').type('WrongPassword123!');
      cy.get('input[placeholder="Enter strong new password"]').type(newPassword);
      cy.get('input[placeholder="Repeat new password"]').type(newPassword);
      
      cy.get('button').contains('Update Password').click();

      cy.get('.auth-error-banner').should('be.visible');
    });
  });

  describe('Manage Profile Picture (RP-20)', () => {
    let currentUserEmail;
    
    beforeEach(() => {
      currentUserEmail = generateRandomEmail();
      cy.registerViaApi({
        email: currentUserEmail,
        password: 'Password123!',
        role: 'DONOR',
        businessOrOrgName: 'Cypress Pic Bakery',
        location: '123 Test St'
      }).then(() => {
        cy.loginViaApi(currentUserEmail, 'Password123!');
      });
    });

    it('should show validation error for invalid file type', () => {
      cy.visit('/profile');
      
      // We will create a dummy text file to trigger the validation
      cy.get('input[type="file"]').selectFile({
        contents: Cypress.Buffer.from('Not an image'),
        fileName: 'test.pdf',
        mimeType: 'application/pdf',
      }, { force: true });
      
      cy.get('.auth-error-banner').should('contain', 'Unsupported file format');
    });

    it('should successfully upload and remove a valid image', () => {
      cy.visit('/profile');
      
      // Upload
      cy.fixture('test-image.png', null).then((fileContent) => {
        cy.get('input[type="file"]').selectFile({
          contents: fileContent,
          fileName: 'test-image.png',
          mimeType: 'image/png',
        }, { force: true });
      });
      
      cy.get('.auth-success-banner').should('contain', 'Profile picture updated');
      cy.screenshot('auth-profile-picture-uploaded');
      
      // Wait for success banner to disappear or click remove
      // The application shows a Remove button when picture exists
      cy.get('button').contains('Remove').click();
      
      // Confirm modal
      cy.get('.modal-container button').contains('Remove Photo').click();
      
      cy.get('.auth-success-banner').should('contain', 'Profile picture removed');
      cy.screenshot('auth-profile-picture-removed');
    });
  });
});
