Feature: Admin
As an admin API client
I want to manage users
So that delete permissions are enforced correctly

Scenario: Delete is forbidden for tutor role
    Given a unique registration request with first name "Delete" last name "Target" password "Passw0rd!" role "Tutor"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as "tutor"
    When I delete the registered user
    Then the request fails with status code 403

Scenario: Delete succeeds for admin role
    Given a unique registration request with first name "Delete" last name "Target" password "Passw0rd!" role "Tutor"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as "admin"
    When I delete the registered user
    Then the delete request is successful

Scenario: Delete fails with invalid bearer token
    Given a unique registration request with first name "Delete" last name "Target" password "Passw0rd!" role "Tutor"
    When I submit the user registration request
    Then the registration request is successful
    Given I use an invalid bearer token
    When I delete the registered user
    Then the request fails with status code 401

Scenario: Get users succeeds for admin and returns pagination headers
    Given I am logged in as "admin"
    When I request users page 1 with page size 5
    Then the get users request is successful
    And the pagination headers are returned and match the response

Scenario: Get users is forbidden for tutor role
    Given I am logged in as "tutor"
    When I request users with the API client
    Then the request fails with status code 403

Scenario: Get users fails with invalid bearer token
    Given I use an invalid bearer token
    When I request users with the API client
    Then the request fails with status code 401
