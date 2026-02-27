Feature: Income
As an authenticated API client
I want income endpoints to handle happy and sad paths
So that CRUD, filtering, pagination, and summary rules are verified

Scenario: Owner can perform Income CRUD successfully
    Given I am logged in as "user"
    When I create an income amount 2500.00 currency "GBP" source "Payroll" type "Salary" note "Monthly salary"
    Then the income create request is successful
    When I fetch the created income
    Then the income get request is successful
    When I update the created income amount 2600.00 currency "GBP" source "Payroll" type "Salary" note "Salary update"
    Then the income update request is successful
    When I delete the created income
    Then the income delete request is successful
    When I fetch the created income
    Then the request fails with status code 404
    When I request deleted income recycle bin page 1 with page size 20
    Then the deleted income list request is successful
    And the deleted income list includes the created income
    When I restore the created income
    Then the income restore request is successful
    When I fetch the created income
    Then the income get request is successful

Scenario: Income endpoints reject invalid bearer token
    Given I use an invalid bearer token
    When I request income for period "2026-02"
    Then the request fails with status code 401

Scenario: Filtering and pagination return filtered records and pagination headers
    Given I am logged in as "user"
    And I create incomes for filtering in period "2026-02"
    When I request income for period "2026-02" filtered by source "Client" page 1 with page size 2
    Then the income list request is successful
    And income pagination headers are returned and match the response

Scenario: Pagination totals are preserved when requested page has no rows
    Given I am logged in as "user"
    And I create incomes for filtering in period "2026-02"
    When I request income for period "2026-02" filtered by source "Client" page 999 with page size 2
    Then the income list request is successful
    And the income page is empty and pagination totals remain positive

Scenario: Filtering fails with invalid period format
    Given I am logged in as "user"
    When I request income for period "2026-13"
    Then the request fails with status code 400

Scenario: Owner monthly income summary succeeds
    Given I am logged in as "user"
    And I update my profile to first name "Income" and last name "Owner" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    And I create an income amount 3000.00 currency "GBP" source "Payroll" type "Salary" note "Summary income"
    When I request income monthly summary for period "2026-02"
    Then the income monthly summary request is successful
    And the income monthly summary total amount is greater than 0

Scenario: Admin can read user monthly income summary
    Given I am logged in as "user"
    And I capture my current user id for income
    And I create an income amount 1900.00 currency "GBP" source "Client A" type "Freelance" note "Admin read"
    Given I am logged in as "admin"
    When I request admin income monthly summary for the captured user and period "2026-02"
    Then the admin income monthly summary request is successful

Scenario: Non-admin is forbidden from admin income endpoints
    Given I am logged in as "user"
    And I capture my current user id for income
    When I request admin income monthly summary for the captured user and period "2026-02"
    Then the request fails with status code 403

Scenario: User cannot access another user's income by id
    Given I am logged in as "user"
    And I create an income amount 1450.00 currency "GBP" source "Client B" type "Freelance" note "Ownership check"
    Given I am logged in as "admin"
    When I fetch the created income
    Then the request fails with status code 404
    When I update the created income amount 1500.00 currency "GBP" source "Client B" type "Freelance" note "Ownership update"
    Then the request fails with status code 404
    When I delete the created income
    Then the request fails with status code 404

Scenario: Another user cannot see or restore deleted income
    Given I am logged in as "user"
    And I create an income amount 1450.00 currency "GBP" source "Client B" type "Freelance" note "Ownership recycle bin"
    When I delete the created income
    Then the income delete request is successful
    Given I am logged in as "admin"
    When I request deleted income recycle bin page 1 with page size 20
    Then the deleted income list request is successful
    And the deleted income list does not include the created income
    When I restore the created income
    Then the request fails with status code 404

Scenario: Income create fails when currency mismatches user profile currency
    Given I am logged in as "user"
    And I update my profile to first name "Income" and last name "Mismatch" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    When I create an income amount 2200.00 currency "USD" source "Payroll" type "Salary" note "Currency mismatch"
    Then the request fails with status code 400

Scenario: Admin income summary fails with invalid period format
    Given I am logged in as "user"
    And I capture my current user id for income
    Given I am logged in as "admin"
    When I request admin income monthly summary for the captured user and period "2026-13"
    Then the request fails with status code 400

Scenario: Admin income summary fails for non-existent user
    Given I am logged in as "admin"
    When I request admin income monthly summary for a non-existent user and period "2026-02"
    Then the request fails with status code 404
