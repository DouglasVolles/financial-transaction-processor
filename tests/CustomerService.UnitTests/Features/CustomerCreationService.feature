Feature: Customer creation service
  As the customer creation pipeline
  I want to validate and persist customers consistently
  So that only valid and unique records are stored

  Scenario: Create a customer successfully
    Given a clean customer database
    And a valid customer request with name "Maria Silva" and cpfCnpj "123.456.789-01"
    When I create the customer
    Then the creation status should be "Created"
    And one customer should be persisted with normalized cpfCnpj "12345678901"
  
  Scenario: Reject duplicate cpfCnpj
    Given a clean customer database with an existing customer cpfCnpj "12345678901"
    And a valid customer request with name "Another Customer" and cpfCnpj "123.456.789-01"
    When I create the customer
    Then the creation status should be "RejectedDuplicate"
    And no additional customers should be persisted
