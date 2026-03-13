Feature: Customers controller
  As an API consumer
  I want the customer endpoints to return correct responses
  So that I can publish and read customer data reliably

  Scenario: Add customer request publishes to queue
    Given a customers controller with a healthy queue service
    When I post customer request with name "Joao" and cpfCnpj "11111111111"
    Then the add customer response status code should be 200
    And the queue should receive one publish request

  Scenario: Add customer request returns 500 when queue fails
    Given a customers controller with a failing queue service
    When I post customer request with name "Joao" and cpfCnpj "11111111111"
    Then the add customer response status code should be 500

  Scenario: Get customer by id returns customer when found
    Given a customers controller with seeded customer id 7 name "Ana" and cpfCnpj "22222222222"
    When I get customer by id 7
    Then the get customer response status code should be 200
    And the returned customer name should be "Ana"

  Scenario: Get customer by id returns not found when missing
    Given a customers controller with no customers
    When I get customer by id 999
    Then the get customer response status code should be 404

  Scenario: Get all customers returns all persisted customers
    Given a customers controller with 2 seeded customers
    When I get all customers
    Then the get all response status code should be 200
    And the returned customer count should be 2

  Scenario: Get all customers returns empty list when no customers exist
    Given a customers controller with no customers
    When I get all customers
    Then the get all response status code should be 200
    And the returned customer count should be 0
