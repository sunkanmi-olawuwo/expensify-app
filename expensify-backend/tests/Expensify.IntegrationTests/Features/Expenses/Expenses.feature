Feature: Expenses
As an authenticated API client
I want expenses endpoints to handle happy and sad paths
So that CRUD, filtering, pagination, and summary rules are verified

Scenario: Owner can perform Expense CRUD successfully
    Given I am logged in as "user"
    And I create expense category "Food"
    And I create expense tag "Groceries"
    When I create an expense amount 25.50 currency "GBP" merchant "Tesco" note "Weekly shop" payment method "DirectDebit"
    Then the expense create request is successful
    When I fetch the created expense
    Then the expense get request is successful
    When I update the created expense amount 30.00 currency "GBP" merchant "Tesco Extra" note "Weekly shop updated" payment method "Card"
    Then the expense update request is successful
    When I delete the created expense
    Then the expense delete request is successful
    When I fetch the created expense
    Then the request fails with status code 404

Scenario: Expense endpoints reject invalid bearer token
    Given I use an invalid bearer token
    When I request expenses for period "2026-02"
    Then the request fails with status code 401

Scenario: Filtering and pagination return filtered records and pagination headers
    Given I am logged in as "user"
    And I create expense category "Food"
    And I create expense tag "Household"
    And I create expenses for filtering in period "2026-02"
    When I request expenses for period "2026-02" filtered by merchant "Tesco" page 1 with page size 2
    Then the expenses list request is successful
    And expense pagination headers are returned and match the response
    And all listed expenses contain merchant text "Tesco"

Scenario: Filtering fails with invalid period format
    Given I am logged in as "user"
    When I request expenses for period "2026-13"
    Then the request fails with status code 400

Scenario: Owner monthly summary succeeds
    Given I am logged in as "user"
    And I update my profile to first name "Summary" and last name "Owner" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create expense category "SummaryCategory"
    And I create expense tag "SummaryTag"
    And I create an expense amount 80.00 currency "GBP" merchant "Summary Merchant" note "Summary expense" payment method "Card"
    When I request monthly summary for period "2026-02"
    Then the monthly summary request is successful
    And the monthly summary total amount is greater than 0

Scenario: Admin can read user monthly summary
    Given I am logged in as "user"
    And I capture my current user id
    And I create expense category "AdminReadCategory"
    And I create expense tag "AdminReadTag"
    And I create an expense amount 60.00 currency "GBP" merchant "Admin Read Merchant" note "Admin read" payment method "Card"
    Given I am logged in as "admin"
    When I request admin monthly summary for the captured user and period "2026-02"
    Then the admin monthly summary request is successful

Scenario: Non-admin is forbidden from admin read endpoints
    Given I am logged in as "user"
    And I capture my current user id
    When I request admin monthly summary for the captured user and period "2026-02"
    Then the request fails with status code 403
