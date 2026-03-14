Feature: Accounts API integration
  As an API consumer
  I want account endpoints to work through the full HTTP pipeline
  So that requests, validation, and queue publication integrate correctly

  Scenario: Get all accounts returns empty list
    Given the account database is empty
    When I send GET request to "/api/financialtransaction/accounts"
    Then the response status code should be 200
    And the response should contain 0 accounts

  Scenario: Get account by id returns account for existing id
    Given the account database has account id 1, customer id 500, identification "ACC-001", available balance 2000.00, reserved balance 150.00, credit limit 10000.00 and status "Active"
    When I send GET request to "/api/financialtransaction/accounts/1"
    Then the response status code should be 200
    And the response account id should be 1
    And the response customer id should be 500
    And the response identification should be "ACC-001"
    And the response available balance should be 2000.00
    And the response reserved balance should be 150.00
    And the response credit limit should be 10000.00
    And the response account status should be "Active"

  Scenario: Add account publishes message to accounts queue
    Given the account queue is empty
    When I send POST request to "/api/financialtransaction/accounts" with customer cpfcnpj "987.654.321-00", available balance 5000.00, reserved balance 300.00, credit limit 15000.00 and status "Active"
    Then the response status code should be 200
    And the queue should contain 1 published account message
    And the last published account queue name should be "accounts"

  Scenario: Add account with negative credit limit returns validation error
    Given the account queue is empty
    When I send POST request to "/api/financialtransaction/accounts" with customer cpfcnpj "111.222.333-44", available balance 10.00, reserved balance 0.00, credit limit -100.00 and status "Inactive"
    Then the response status code should be 400
    And the response should contain validation error for field "CreditLimit"
    And the queue should contain 0 published account message
