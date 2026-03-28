Feature: Dashboard
As an authenticated API client
I want the dashboard summary endpoint to aggregate the dashboard in one request
So that hero metrics, charts, and recent activity stay consistent

Scenario: Dashboard summary returns aggregated metrics for the authenticated user
    Given a unique registration request with first name "Dashboard" last name "Owner" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Dashboard" and last name "Owner" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create dashboard expense category "Travel"
    And I create a dashboard expense amount 120.00 currency "GBP" category "Food" merchant "Tesco" note "Groceries" payment method "Card" on "2026-03-10"
    And I create a dashboard expense amount 80.00 currency "GBP" category "Travel" merchant "Trainline" note "Commute" payment method "Card" on "2026-03-12"
    And I create a dashboard expense amount 100.00 currency "GBP" category "Food" merchant "Tesco" note "Previous groceries" payment method "Card" on "2026-02-10"
    And I create a dashboard income amount 1000.00 currency "GBP" source "Payroll" type "Salary" note "Salary" on "2026-03-05"
    And I create a dashboard income amount 500.00 currency "GBP" source "Client A" type "Freelance" note "Project" on "2026-03-14"
    And I create a dashboard income amount 1000.00 currency "GBP" source "Payroll" type "Salary" note "Previous salary" on "2026-02-03"
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard monthly income is 1500.00 currency "GBP" with change 50.00
    And dashboard monthly expenses is 200.00 currency "GBP" with change 100.00
    And dashboard net cash flow is 1300.00 currency "GBP" with change 44.44
    And dashboard spending breakdown contains 2 entries
    And dashboard spending breakdown contains category "Food" amount 120.00 percentage 60.00 color key "chart-1"
    And dashboard spending breakdown contains category "Travel" amount 80.00 percentage 40.00 color key "chart-2"
    And dashboard monthly performance contains 6 entries
    And dashboard monthly performance month "Feb 2026" has income 1000.00 and expenses 100.00
    And dashboard monthly performance month "Mar 2026" has income 1500.00 and expenses 200.00

Scenario: Dashboard summary rejects invalid bearer token
    Given I use an invalid bearer token
    When I request dashboard summary
    Then the request fails with status code 401

Scenario: Dashboard summary returns zeroed data for a newly registered user
    Given a unique registration request with first name "Zero" last name "Dashboard" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard monthly income is 0.00 currency "GBP" with change 0.00
    And dashboard monthly expenses is 0.00 currency "GBP" with change 0.00
    And dashboard net cash flow is 0.00 currency "GBP" with change 0.00
    And dashboard spending breakdown is empty
    And dashboard recent transactions are empty
    And dashboard monthly performance contains 6 zeroed entries

Scenario: Dashboard summary returns zero change percentages when only the current month has data
    Given a unique registration request with first name "Current" last name "Only" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Current" and last name "Only" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create a dashboard expense amount 50.00 currency "GBP" category "Food" merchant "Corner Shop" note "Lunch" payment method "Card" on "2026-03-08"
    And I create a dashboard income amount 200.00 currency "GBP" source "Client B" type "Freelance" note "One-off" on "2026-03-09"
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard monthly income is 200.00 currency "GBP" with change 0.00
    And dashboard monthly expenses is 50.00 currency "GBP" with change 0.00
    And dashboard net cash flow is 150.00 currency "GBP" with change 0.00

Scenario: Dashboard recent transactions are capped at five and ordered newest first
    Given a unique registration request with first name "Recent" last name "Dashboard" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Recent" and last name "Dashboard" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create a dashboard expense amount 10.00 currency "GBP" category "Food" merchant "One" note "One" payment method "Card" on "2026-03-01"
    And I create a dashboard income amount 20.00 currency "GBP" source "Two" type "Salary" note "Two" on "2026-03-02"
    And I create a dashboard expense amount 30.00 currency "GBP" category "Food" merchant "Three" note "Three" payment method "Card" on "2026-03-03"
    And I create a dashboard income amount 40.00 currency "GBP" source "Four" type "Freelance" note "Four" on "2026-03-04"
    And I create a dashboard expense amount 50.00 currency "GBP" category "Food" merchant "Five" note "Five" payment method "Card" on "2026-03-05"
    And I create a dashboard income amount 60.00 currency "GBP" source "Six" type "Other" note "Six" on "2026-03-06"
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard recent transactions contain 5 entries
    And dashboard recent transactions are ordered newest first

Scenario: Dashboard summary is scoped to the authenticated user
    Given I am logged in as "user"
    And I update my profile to first name "Owner" and last name "Dashboard" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create a dashboard expense amount 75.00 currency "GBP" category "Food" merchant "Owner Merchant" note "Owner" payment method "Card" on "2026-03-10"
    And I create a dashboard income amount 300.00 currency "GBP" source "Owner Source" type "Salary" note "Owner" on "2026-03-11"
    Given a unique registration request with first name "Other" last name "Dashboard" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard recent transactions do not include the created dashboard transaction ids
