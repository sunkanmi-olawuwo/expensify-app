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
    And I create a dashboard expense amount 120.00 currency "GBP" category "Food" merchant "Tesco" note "Groceries" payment method "Card" on "~0/10"
    And I create a dashboard expense amount 80.00 currency "GBP" category "Travel" merchant "Trainline" note "Commute" payment method "Card" on "~0/12"
    And I create a dashboard expense amount 100.00 currency "GBP" category "Food" merchant "Tesco" note "Previous groceries" payment method "Card" on "~1/10"
    And I create a dashboard income amount 1000.00 currency "GBP" source "Payroll" type "Salary" note "Salary" on "~0/05"
    And I create a dashboard income amount 500.00 currency "GBP" source "Client A" type "Freelance" note "Project" on "~0/14"
    And I create a dashboard income amount 1000.00 currency "GBP" source "Payroll" type "Salary" note "Previous salary" on "~1/03"
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard monthly income is 1500.00 currency "GBP" with change 50.00
    And dashboard monthly expenses is 200.00 currency "GBP" with change 100.00
    And dashboard net cash flow is 1300.00 currency "GBP" with change 44.44
    And dashboard spending breakdown contains 2 entries
    And dashboard spending breakdown contains category "Food" amount 120.00 percentage 60.00 color key "chart-1"
    And dashboard spending breakdown contains category "Travel" amount 80.00 percentage 40.00 color key "chart-2"
    And dashboard monthly performance contains 6 entries
    And dashboard monthly performance month "~1" has income 1000.00 and expenses 100.00
    And dashboard monthly performance month "~0" has income 1500.00 and expenses 200.00

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
    And I create a dashboard expense amount 50.00 currency "GBP" category "Food" merchant "Corner Shop" note "Lunch" payment method "Card" on "~0/08"
    And I create a dashboard income amount 200.00 currency "GBP" source "Client B" type "Freelance" note "One-off" on "~0/09"
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
    And I create a dashboard expense amount 10.00 currency "GBP" category "Food" merchant "One" note "One" payment method "Card" on "~0/01"
    And I create a dashboard income amount 20.00 currency "GBP" source "Two" type "Salary" note "Two" on "~0/02"
    And I create a dashboard expense amount 30.00 currency "GBP" category "Food" merchant "Three" note "Three" payment method "Card" on "~0/03"
    And I create a dashboard income amount 40.00 currency "GBP" source "Four" type "Freelance" note "Four" on "~0/04"
    And I create a dashboard expense amount 50.00 currency "GBP" category "Food" merchant "Five" note "Five" payment method "Card" on "~0/05"
    And I create a dashboard income amount 60.00 currency "GBP" source "Six" type "Other" note "Six" on "~0/06"
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard recent transactions contain 5 entries
    And dashboard recent transactions are ordered newest first

Scenario: Dashboard summary is scoped to the authenticated user
    Given I am logged in as "user"
    And I update my profile to first name "Owner" and last name "Dashboard" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create a dashboard expense amount 75.00 currency "GBP" category "Food" merchant "Owner Merchant" note "Owner" payment method "Card" on "~0/10"
    And I create a dashboard income amount 300.00 currency "GBP" source "Owner Source" type "Salary" note "Owner" on "~0/11"
    Given a unique registration request with first name "Other" last name "Dashboard" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    When I request dashboard summary
    Then the dashboard summary request is successful
    And dashboard recent transactions do not include the created dashboard transaction ids

Scenario: Dashboard analytics return cash flow, income breakdown, and top categories
    Given a unique registration request with first name "Analytics" last name "Owner" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Analytics" and last name "Owner" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create dashboard expense category "Travel"
    And I create dashboard expense category "Rent"
    And I create a dashboard expense amount 300.00 currency "GBP" category "Rent" merchant "Landlord" note "January rent" payment method "Card" on "~2/02"
    And I create a dashboard expense amount 200.00 currency "GBP" category "Food" merchant "Tesco" note "February groceries" payment method "Card" on "~1/10"
    And I create a dashboard expense amount 150.00 currency "GBP" category "Food" merchant "Tesco" note "March groceries" payment method "Card" on "~0/10"
    And I create a dashboard expense amount 50.00 currency "GBP" category "Travel" merchant "Trainline" note "March commute" payment method "Card" on "~0/11"
    And I create a dashboard expense amount 400.00 currency "GBP" category "Rent" merchant "Landlord" note "March rent" payment method "Card" on "~0/02"
    And I create a dashboard income amount 1000.00 currency "GBP" source "Payroll" type "Salary" note "January salary" on "~2/05"
    And I create a dashboard income amount 1500.00 currency "GBP" source "Payroll" type "Salary" note "February salary" on "~1/05"
    And I create a dashboard income amount 1200.00 currency "GBP" source "Payroll" type "Salary" note "March salary" on "~0/05"
    And I create a dashboard income amount 500.00 currency "GBP" source "Client A" type "Freelance" note "March freelance" on "~0/15"
    When I request dashboard cash flow trend with months 3
    Then the dashboard cash flow trend request is successful
    And dashboard cash flow trend contains 3 months
    And dashboard cash flow month "~2" has income 1000.00 expenses 300.00 net cash flow 700.00 savings rate 70.00
    And dashboard cash flow month "~1" has income 1500.00 expenses 200.00 net cash flow 1300.00 savings rate 86.67
    And dashboard cash flow month "~0" has income 1700.00 expenses 600.00 net cash flow 1100.00 savings rate 64.71
    When I request dashboard income breakdown with months 3
    Then the dashboard income breakdown request is successful
    And dashboard income breakdown period is "Last 3 months" currency "GBP" total income 4200.00
    And dashboard income breakdown contains 2 sources
    And dashboard income breakdown contains source "Salary" amount 3700.00 percentage 88.10 color key "chart-1"
    And dashboard income breakdown contains source "Freelance" amount 500.00 percentage 11.90 color key "chart-2"
    When I request dashboard top categories with months 3 and limit 2
    Then the dashboard top categories request is successful
    And dashboard top categories period "Last 3 months" currency "GBP" total spent 1100.00
    And dashboard top categories contains 2 entries
    And dashboard top categories contains rank 1 category "Rent" amount 700.00 percentage 63.64 color key "chart-1"
    And dashboard top categories contains rank 2 category "Food" amount 350.00 percentage 31.82 color key "chart-2"

Scenario: Dashboard category comparison respects month start boundaries
    Given a unique registration request with first name "Compare" last name "Dashboard" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Compare" and last name "Dashboard" currency "GBP" timezone "UTC" month start day 5
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create dashboard expense category "Travel"
    And I create a dashboard expense amount 80.00 currency "GBP" category "Food" merchant "Tesco" note "January current" payment method "Card" on "~2/10"
    And I create a dashboard expense amount 30.00 currency "GBP" category "Food" merchant "Tesco" note "Boundary previous" payment method "Card" on "~1/04"
    And I create a dashboard expense amount 120.00 currency "GBP" category "Food" merchant "Tesco" note "February current" payment method "Card" on "~1/10"
    And I create a dashboard expense amount 50.00 currency "GBP" category "Travel" merchant "Trainline" note "Travel current" payment method "Card" on "~1/20"
    And I create a dashboard expense amount 25.00 currency "GBP" category "Travel" merchant "Trainline" note "Boundary current" payment method "Card" on "~0/04"
    When I request dashboard category comparison for month "~1"
    Then the dashboard category comparison request is successful
    And dashboard category comparison current month "~1" previous month "~2" currency "GBP"
    And dashboard category comparison contains 2 categories
    And dashboard category comparison contains category "Food" current amount 120.00 previous amount 110.00 change amount 10.00 change percentage 9.09
    And dashboard category comparison contains category "Travel" current amount 75.00 previous amount 0.00 change amount 75.00 change percentage 0.00

Scenario: Dashboard investment analytics return allocation and trend data
    Given a unique registration request with first name "Invest" last name "Dashboard" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Invest" and last name "Dashboard" currency "GBP" timezone "Europe/Berlin" month start day 1
    Then the update profile request is successful
    Given I create a dashboard investment account "ISA Main" in category slug "isa" with current balance 1500.00
    And I create a dashboard investment account "LISA Main" in category slug "lisa" with current balance 500.00
    And I add a dashboard investment contribution amount 200.00 to account "ISA Main" at "~2/15T10:00:00Z" note "January top up"
    And I add a dashboard investment contribution amount 100.00 to account "ISA Main" at "~1/15T23:30:00Z" note "Month boundary top up"
    And I add a dashboard investment contribution amount 50.00 to account "LISA Main" at "~0/10T08:00:00Z" note "March top up"
    When I request dashboard investment allocation
    Then the dashboard investment allocation request is successful
    And dashboard investment allocation currency "GBP" total value 2000.00 account count 2
    And dashboard investment allocation contains 2 categories
    And dashboard investment allocation contains category "ISA" slug "isa" total balance 1500.00 account count 1 percentage 75.00 color key "chart-1"
    And dashboard investment allocation contains category "LISA" slug "lisa" total balance 500.00 account count 1 percentage 25.00 color key "chart-2"
    When I request dashboard investment trend with months 3
    Then the dashboard investment trend request is successful
    And dashboard investment trend currency "GBP" total contributed 350.00
    And dashboard investment trend contains 3 months
    And dashboard investment trend month "~2" has contributions 200.00 and account count 1
    And dashboard investment trend month "~1" has contributions 100.00 and account count 1
    And dashboard investment trend month "~0" has contributions 50.00 and account count 1

Scenario: Dashboard analytics reject invalid bearer token
    Given I use an invalid bearer token
    When I request dashboard cash flow trend with months 3
    Then the request fails with status code 401

Scenario: Dashboard analytics return empty data for a newly registered user
    Given a unique registration request with first name "Empty" last name "Analytics" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    When I request dashboard cash flow trend with months 3
    Then the dashboard cash flow trend request is successful
    And dashboard cash flow trend contains 3 zeroed months
    When I request dashboard income breakdown with months 3
    Then the dashboard income breakdown request is successful
    And dashboard income breakdown period is "Last 3 months" currency "GBP" total income 0.00
    And dashboard income breakdown sources are empty
    When I request dashboard category comparison for month "~0"
    Then the dashboard category comparison request is successful
    And dashboard category comparison categories are empty
    When I request dashboard top categories with months 3 and limit 5
    Then the dashboard top categories request is successful
    And dashboard top categories period "Last 3 months" currency "GBP" total spent 0.00
    And dashboard top categories are empty
    When I request dashboard investment allocation
    Then the dashboard investment allocation request is successful
    And dashboard investment allocation currency "GBP" total value 0.00 account count 0
    And dashboard investment allocation categories are empty
    When I request dashboard investment trend with months 3
    Then the dashboard investment trend request is successful
    And dashboard investment trend currency "GBP" total contributed 0.00
    And dashboard investment trend contains 3 zeroed months

Scenario: Dashboard analytics fall back to documented defaults for invalid query parameters
    Given a unique registration request with first name "Default" last name "Analytics" password "Passw0rd!" role "User"
    When I submit the user registration request
    Then the registration request is successful
    Given I am logged in as the newly registered user
    And I update my profile to first name "Default" and last name "Analytics" currency "GBP" timezone "UTC" month start day 1
    Then the update profile request is successful
    Given I create dashboard expense category "Food"
    And I create a dashboard expense amount 120.00 currency "GBP" category "Food" merchant "Tesco" note "Fallback groceries" payment method "Card" on "~0/10"
    And I create a dashboard income amount 800.00 currency "GBP" source "Payroll" type "Salary" note "Fallback salary" on "~0/05"
    And I create a dashboard investment account "Fallback ISA" in category slug "isa" with current balance 1000.00
    And I add a dashboard investment contribution amount 75.00 to account "Fallback ISA" at "~0/12T08:00:00Z" note "Fallback contribution"
    When I request dashboard cash flow trend with months 99
    Then the dashboard cash flow trend request is successful
    And dashboard cash flow trend contains 6 months
    When I request dashboard income breakdown with months 99
    Then the dashboard income breakdown request is successful
    And dashboard income breakdown period is "Last 3 months" currency "GBP" total income 800.00
    When I request dashboard category comparison for month "invalid-month"
    Then the dashboard category comparison request is successful
    And dashboard category comparison current month "~0" previous month "~1" currency "GBP"
    When I request dashboard top categories with months 99 and limit 0
    Then the dashboard top categories request is successful
    And dashboard top categories period "Last 3 months" currency "GBP" total spent 120.00
    And dashboard top categories contains 1 entries
    When I request dashboard investment trend with months 99
    Then the dashboard investment trend request is successful
    And dashboard investment trend contains 6 months
