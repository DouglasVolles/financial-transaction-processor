Feature: Transaction processor
  As the transaction processing pipeline
  I want to process financial transactions correctly
  So that account balances reflect each operation

  Scenario: Reserve operation followed by capture
    Given an account with identification "ACC-001", available balance 0.00, reserved balance 0.00 and credit limit 500.00
    When I process a transaction with operation "credit", account id "ACC-001", amount 100000, currency "BRL" and reference id "TXN-CREDIT-RESERVE-CAPTURE-001"
    Then the transaction status should be "Success"
    And the transaction available balance should be 100000
    And the transaction reserved balance should be 0
    When I process a transaction with operation "reserve", account id "ACC-001", amount 30000, currency "BRL" and reference id "TXN-RESERVE-001"
    Then the transaction status should be "Success"
    And the transaction available balance should be 70000
    And the transaction reserved balance should be 30000
    When I process a transaction with operation "capture", account id "ACC-001", amount 30000, currency "BRL" and reference id "TXN-CAPTURE-001"
    Then the transaction status should be "Success"
    And the transaction available balance should be 70000
    And the transaction reserved balance should be 0

