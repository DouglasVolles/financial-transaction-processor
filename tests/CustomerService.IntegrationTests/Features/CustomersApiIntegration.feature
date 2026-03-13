Feature: Customers API integration
  As an API consumer
  I want the customer endpoints to work through the full HTTP pipeline
  So that requests, filters, controllers, and persistence integrate correctly

  Scenario: Get all customers returns empty list when database has no customers
    Given the customer database is empty
    When I send GET request to "/api/financialtransaction/customers"
    Then the response status code should be 200
    And the response should contain 0 customers

  Scenario: Get all customers returns seeded customers
    Given the customer database has 2 customers
    When I send GET request to "/api/financialtransaction/customers"
    Then the response status code should be 200
    And the response should contain 2 customers

  Scenario: Get customer by id returns not found for unknown customer
    Given the customer database is empty
    When I send GET request to "/api/financialtransaction/customers/999"
    Then the response status code should be 404

  Scenario: Get customer by id returns customer for existing id
    Given the customer database has customer id 7 name "Ana" and cpfCnpj "22222222222"
    When I send GET request to "/api/financialtransaction/customers/7"
    Then the response status code should be 200
    And the response customer name should be "Ana"

  Scenario: Add customer publishes message through integration pipeline
    Given the customer queue is empty
    When I send POST request to "/api/financialtransaction/customers" with name "Joao" and cpfCnpj "529.982.247-25"
    Then the response status code should be 200
    And the queue should contain 1 published customer message

  Scenario: Add customer with valid cnpj publishes message through integration pipeline
    Given the customer queue is empty
    When I send POST request to "/api/financialtransaction/customers" with name "Empresa X" and cpfCnpj "04.252.011/0001-10"
    Then the response status code should be 200
    And the queue should contain 1 published customer message

  Scenario: Add customer with invalid cnpj returns validation error and does not publish
    Given the customer queue is empty
    When I send POST request to "/api/financialtransaction/customers" with name "Empresa X" and cpfCnpj "04.252.011/0001-11"
    Then the response status code should be 400
    And the response should contain validation error for field "CpfCnpj"
    And the queue should contain 0 published customer message

  Scenario: Add customer with invalid cpf returns validation error and does not publish
    Given the customer queue is empty
    When I send POST request to "/api/financialtransaction/customers" with name "Joao" and cpfCnpj "529.982.247-21"
    Then the response status code should be 400
    And the response should contain validation error for field "CpfCnpj"
    And the queue should contain 0 published customer message
