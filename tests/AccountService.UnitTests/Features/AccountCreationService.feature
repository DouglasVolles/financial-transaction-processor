Feature: Account creation service
  As the account creation pipeline
  I want to create unique accounts with sequential IDs
  So that account data is stored consistently

  Scenario Outline: Create first account successfully
    Given a clean account database
    And a valid account request with customer cpfcnpj "<customerCpfCnpj>", available balance 1000.50, reserved balance 125.25, credit limit 5000.00 and status "Active"
    When I create the account
    Then the account creation status should be "Created"
    And the created account id should be 1
    And one account should be persisted with id 1, customer id <customerId>, identification "ACC-001", available balance 1000.50, reserved balance 125.25, credit limit 5000.00 and status "Active"

    Examples:
      | customerCpfCnpj    | customerId |
      | 123.456.789-01     | 101        |
      | 12.345.678/0001-95 | 301        |

  Scenario: Create next account after an existing account
    Given a clean account database with existing account id 1, customer id 200, identification "ACC-001", available balance 25.00, reserved balance 5.00, credit limit 100.00 and status "Inactive"
    And a valid account request with customer cpfcnpj "987.654.321-00", available balance 10.00, reserved balance 0.00, credit limit 500.00 and status "Inactive"
    When I create the account
    Then the account creation status should be "Created"
    And the created account id should be 2
    And the account count should be 2
