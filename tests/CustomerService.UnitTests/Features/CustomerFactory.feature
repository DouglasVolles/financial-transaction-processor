Feature: Customer factory mapping
  As the customer domain mapper
  I want to build customer entities from request data
  So that persistence receives correctly mapped records

  Scenario: Factory maps request and timestamp
    Given a customer factory
    When I create customer from name "Paula" cpfCnpj "12345678901" and timestamp "2026-03-12T00:00:00Z"
    Then the created customer name should be "Paula"
    And the created customer cpfCnpj should be "12345678901"
    And the created customer timestamp should be "2026-03-12T00:00:00Z"
