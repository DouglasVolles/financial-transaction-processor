Feature: Transactions API integration
  As an API consumer
  I want transaction endpoints to work through HTTP and queue boundaries
  So that requests are queued and data can be queried

  Scenario: Get all transactions returns empty list
    Given the transaction database is empty
    When I send GET request for transactions to "/api/financialtransaction/transations"
    Then the transaction response status code should be 200
    And the response should contain 0 transactions

  Scenario: Get all transactions returns existing transaction
    Given the transaction database has transaction with reference id "TXN-GET-1" and status "success"
    When I send GET request for transactions to "/api/financialtransaction/transations"
    Then the transaction response status code should be 200
    And the response should contain 1 transactions
    And the response should contain transaction id "TXN-GET-1-PROCESSED"
    And the response should contain transaction status "success"

  Scenario: Add transaction publishes message to transactions queue
    Given the account queue is empty
    When I send POST request to "/api/financialtransaction/transations" with operation "credit", account id "ACC-001", amount 10000, currency "BRL", reference id "TXN-001" and metadata "{\"description\":\"Deposito inicial\"}"
    Then the transaction response status code should be 200
    And the transaction response status should be "pending"
    And the transaction response id should be "TXN-001-PROCESSED"
    And the transaction queue should contain 1 published message
    And the last published transaction queue name should be "transactions"

  Scenario: Add transaction with duplicated reference id returns existing record
    Given the transaction database has transaction with reference id "TXN-REF-1" and status "success"
    When I send POST request to "/api/financialtransaction/transations" with operation "debit", account id "ACC-001", amount 5000, currency "BRL", reference id "TXN-REF-1" and metadata "{\"description\":\"Duplicate\"}"
    Then the transaction response status code should be 200
    And the transaction response status should be "success"
    And the transaction response id should be "TXN-REF-1-PROCESSED"
    And the transaction queue should contain 0 published message
