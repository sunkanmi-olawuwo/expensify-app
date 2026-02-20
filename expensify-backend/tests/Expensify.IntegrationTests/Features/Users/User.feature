Feature: User
As an API client
I want to authenticate and onboard users
So that login and registration workflows work as expected

Scenario: Login with existing seeded user
    Given an existing user email "admin@test.com" with password "Test1234!"
    When I log in with those credentials
    Then the login request is successful

Scenario: Login fails with invalid password
    Given an existing user email "admin@test.com" with password "WrongPassword123!"
    When I log in with those credentials
    Then the request fails with status code 400

Scenario: Register a new tutor user
    Given a unique registration request with first name "New" last name "Tutor" password "Passw0rd!" role "Tutor"
    When I submit the user registration request
    Then the registration request is successful

Scenario: Register fails for duplicate email
    Given a unique registration request with first name "Duplicate" last name "Tutor" password "Passw0rd!" role "Tutor"
    When I submit the user registration request
    Then the registration request is successful
    When I submit the user registration request
    Then the request fails with status code 400

Scenario: Get profile succeeds with valid token
    Given I am logged in as "admin"
    When I request my user profile
    Then the get profile request is successful

Scenario: Get profile fails with invalid token
    Given I use an invalid bearer token
    When I request my user profile
    Then the request fails with status code 401

Scenario: Update profile succeeds with valid token
    Given I am logged in as "admin"
    When I update my profile to first name "AdminUpdated" and last name "UserUpdated"
    Then the update profile request is successful
