Feature: Customer request validation
  As the API validation layer
  I want to reject invalid inputs and allow valid payloads
  So that the controller receives consistent request models

  Scenario: Validator reports errors for empty fields
    Given a customer request validator
    When I validate request with name "" and cpfCnpj ""
    Then validation should fail
    And validation should contain error for field "Name"
    And validation should contain error for field "CpfCnpj"

  Scenario: Validator accepts valid fields
    Given a customer request validator
    When I validate request with name "Valid Name" and cpfCnpj "529.982.247-25"
    Then validation should pass

  Scenario: Validator rejects invalid cpf or cnpj
    Given a customer request validator
    When I validate request with name "Valid Name" and cpfCnpj "12345678901"
    Then validation should fail
    And validation should contain error for field "CpfCnpj"

  Scenario: Validator accepts valid cnpj with mask
    Given a customer request validator
    When I validate request with name "Valid Name" and cpfCnpj "04.252.011/0001-10"
    Then validation should pass

  Scenario: Validator rejects invalid cnpj
    Given a customer request validator
    When I validate request with name "Valid Name" and cpfCnpj "04.252.011/0001-11"
    Then validation should fail
    And validation should contain error for field "CpfCnpj"

  Scenario: Validator rejects repeated-digit cpf
    Given a customer request validator
    When I validate request with name "Valid Name" and cpfCnpj "11111111111"
    Then validation should fail
    And validation should contain error for field "CpfCnpj"

  Scenario: Validator rejects repeated-digit cnpj
    Given a customer request validator
    When I validate request with name "Valid Name" and cpfCnpj "11111111111111"
    Then validation should fail
    And validation should contain error for field "CpfCnpj"

  Scenario: Validation filter blocks invalid action argument
    Given a validation filter configured with customer validator
    And an invalid action argument with name "" and cpfCnpj ""
    When the validation filter executes
    Then the filter response status code should be 400
    And the action delegate should not be executed

  Scenario: Validation filter allows valid action argument
    Given a validation filter configured with customer validator
    And a valid action argument with name "Valid Name" and cpfCnpj "529.982.247-25"
    When the validation filter executes
    Then the filter should continue to action delegate
