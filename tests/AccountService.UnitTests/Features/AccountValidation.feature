Feature: Account request validation
  As an API validator
  I want to validate account request payloads
  So that invalid data is rejected early

  Scenario: Reject negative reserved balance
    Given an account request with customer cpfcnpj "123.456.789-01", available balance 100.00, reserved balance -10.00, credit limit 1000.00 and status "Active"
    When I validate the account request
    Then the account request should be invalid
    And the validation should contain error for field "ReservedBalance"

  Scenario: Reject empty customer cpfcnpj
    Given an account request with customer cpfcnpj "", available balance 100.00, reserved balance 10.00, credit limit 1000.00 and status "Blocked"
    When I validate the account request
    Then the account request should be invalid
    And the validation should contain error for field "CustomerCpFCnpj"

  Scenario: Reject negative available balance
    Given an account request with customer cpfcnpj "987.654.321-00", available balance -1.00, reserved balance 10.00, credit limit 1000.00 and status "Blocked"
    When I validate the account request
    Then the account request should be invalid
    And the validation should contain error for field "AvailableBalance"

  Scenario: Accept valid account data
    Given an account request with customer cpfcnpj "123.456.789-01", available balance 2500.00, reserved balance 150.00, credit limit 10000.00 and status "Active"
    When I validate the account request
    Then the account request should be valid
