Feature: User
As an API client
I want to authenticate and manage my credentials
So that login, registration, and password workflows work as expected

Scenario: Login with existing seeded user
    Given an existing user email "admin@test.com" with password "Test1234!"
    When I log in with those credentials
    Then the login request is successful

Scenario: Login fails with invalid password
    Given an existing user email "admin@test.com" with password "WrongPassword123!"
    When I log in with those credentials
    Then the request fails with status code 400

Scenario: Register a new user user
    Given a unique registration request with first name "New" last name "User" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful

Scenario: Register fails for duplicate email
    Given a unique registration request with first name "Duplicate" last name "User" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    When I submit the user registration request
    Then the request fails with status code 400

Scenario: Logout invalidates all active sessions
    Given a unique registration request with first name "Logout" last name "User" password "Test1234!" role "User"
    And I submit the user registration request
    And the registration request is successful
    And I am logged in as the newly registered user
    And I also log in as the newly registered user in a secondary session
    When I log out of the current account
    Then the logout request is successful
    And the current session is rejected when I request my user profile
    And the secondary session is rejected when I request my user profile
    And refreshing the secondary session fails

Scenario: Change password succeeds and invalidates existing sessions
    Given a unique registration request with first name "Password" last name "Success" password "Test1234!" role "User"
    And I submit the user registration request
    And the registration request is successful
    And I am logged in as the newly registered user
    And I also log in as the newly registered user in a secondary session
    When I change my password from "Test1234!" to "NewPassword123!"
    Then the change password request is successful
    And the current session is rejected when I request my user profile
    And the secondary session is rejected when I request my user profile
    Given the newly registered user email with password "Test1234!"
    When I log in with those credentials
    Then the request fails with status code 400
    Given the newly registered user email with password "NewPassword123!"
    When I log in with those credentials
    Then the login request is successful

Scenario: Change password fails with invalid current password
    Given a unique registration request with first name "Password" last name "Failure" password "Test1234!" role "User"
    And I submit the user registration request
    And the registration request is successful
    And I am logged in as the newly registered user
    When I change my password from "WrongPassword123!" to "NewPassword123!"
    Then the request fails with status code 400

Scenario: Forgot password returns success for existing email
    Given a unique registration request with first name "Forgot" last name "Known" password "Test1234!" role "User"
    And I submit the user registration request
    And the registration request is successful
    When I request a password reset for the newly registered user email
    Then the forgot password request is successful
    And a password reset link is captured for the newly registered user email

Scenario: Forgot password returns success for unknown email
    When I request a password reset for email "missing@test.com"
    Then the forgot password request is successful
    And no password reset link is captured for email "missing@test.com"

Scenario: Reset password succeeds with captured token
    Given a unique registration request with first name "Reset" last name "Success" password "Test1234!" role "User"
    And I submit the user registration request
    And the registration request is successful
    And I am logged in as the newly registered user
    And I also log in as the newly registered user in a secondary session
    And I request a password reset for the newly registered user email
    And a password reset link is captured for the newly registered user email
    When I reset the password for the newly registered user to "BrandNew123!" using the captured reset token
    Then the reset password request is successful
    And the current session is rejected when I request my user profile
    And the secondary session is rejected when I request my user profile
    Given the newly registered user email with password "Test1234!"
    When I log in with those credentials
    Then the request fails with status code 400
    Given the newly registered user email with password "BrandNew123!"
    When I log in with those credentials
    Then the login request is successful

Scenario: Reset password fails with invalid token
    Given a unique registration request with first name "Reset" last name "Failure" password "Test1234!" role "User"
    And I submit the user registration request
    And the registration request is successful
    When I reset the password for the newly registered user to "BrandNew123!" using token "invalid-token"
    Then the request fails with status code 400

Scenario: Get profile succeeds with valid token
    Given I am logged in as "admin"
    When I request my user profile
    Then the get profile request is successful

Scenario: Get profile fails with invalid token
    Given I use an invalid bearer token
    When I request my user profile
    Then the request fails with status code 401

Scenario: Get profile fails with revoked token
    Given I am logged in as "admin"
    And my current access token is revoked
    When I request my user profile
    Then the request fails with status code 401

Scenario: Update profile succeeds with valid token
    Given I am logged in as "admin"
    When I update my profile to first name "AdminUpdated" and last name "UserUpdated" currency "EUR" timezone "Europe/Berlin" month start day 5
    Then the update profile request is successful

Scenario: Update profile fails with invalid month start day
    Given I am logged in as "admin"
    When I update my profile to first name "AdminUpdated" and last name "UserUpdated" currency "EUR" timezone "Europe/Berlin" month start day 30
    Then the request fails with status code 400

Scenario: Update profile fails with invalid currency format
    Given I am logged in as "admin"
    When I update my profile to first name "AdminUpdated" and last name "UserUpdated" currency "usd" timezone "Europe/Berlin" month start day 5
    Then the request fails with status code 400

Scenario: Get profile returns updated user preferences
    Given I am logged in as "admin"
    When I update my profile to first name "Pref" and last name "Check" currency "GBP" timezone "Europe/London" month start day 7
    Then the update profile request is successful
    When I request my user profile
    Then the get profile contains currency "GBP" timezone "Europe/London" and month start day 7

Scenario: Auth write endpoints are rate limited
    Given an existing user email "admin@test.com" with password "Test1234!"
    When I attempt to log in with those credentials 12 times
    Then the request fails with status code 429
    And the error response contains title "RateLimit.Exceeded"
