Feature: Credit transaction rule handler
  As the credit transaction rule
  I want to increase the account available balance on credit operations
  So that deposits are correctly reflected in the account

  Scenario: Credit increases available balance
    Given a source account with available balance 50.00, reserved balance 0.00 and credit limit 0.00
    When I apply the "credit" rule handler with operation "credit" and amount 25.00
    Then the rule result should succeed
    And the source account available balance should be 75.00

  Scenario: Credit does not affect reserved balance
    Given a source account with available balance 50.00, reserved balance 10.00 and credit limit 0.00
    When I apply the "credit" rule handler with operation "credit" and amount 20.00
    Then the rule result should succeed
    And the source account reserved balance should be 10.00

  Scenario: Credit handler rejects an unsupported operation type
    Given a source account with available balance 50.00, reserved balance 0.00 and credit limit 0.00
    When I apply the "credit" rule handler with operation "debit" and amount 10.00
    Then the rule result should fail with message containing "Unsupported transaction operation"

  Scenario: Credit transaction is idempotent by reference id in processor flow
    Given an account with identification "ACC-001", available balance 100.00, reserved balance 10.00 and credit limit 1000.00
    When I process the same transaction twice with operation "credit", account id "ACC-001", amount 10000, currency "BRL" and reference id "TXN-001"
    Then the first transaction status should be "Success"
    And the second transaction status should be "Success"
    And both transaction ids should be equal
    And the persisted transaction count should be 1

  Scenario: Credit transaction fails when account does not exist in processor flow
    Given a clean transaction processor context
    When I process a transaction with operation "credit", account id "ACC-404", amount 10000, currency "BRL" and reference id "TXN-404"
    Then the transaction status should be "Failed"
    And the transaction error message should be "Account not found"
    And the persisted transaction count should be 0

  Scenario: Credit transaction succeeds and updates available balance in processor flow
    Given an account with identification "ACC-CREDIT", available balance 50.00, reserved balance 0.00 and credit limit 0.00
    When I process a transaction with operation "credit", account id "ACC-CREDIT", amount 2500, currency "BRL" and reference id "TXN-CREDIT-OK"
    Then the transaction status should be "Success"
    And the transaction available balance should be 7500
