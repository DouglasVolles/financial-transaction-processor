Feature: Debit transaction rule handler
  As the debit transaction rule
  I want to deduct from the account available balance on debit operations
  So that withdrawals respect available funds and credit limits

  Scenario: Debit deducts from available balance
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I apply the "debit" rule handler with operation "debit" and amount 40.00
    Then the rule result should succeed
    And the source account available balance should be 60.00

  Scenario: Debit succeeds using the credit limit
    Given a source account with available balance 10.00, reserved balance 0.00 and credit limit 50.00
    When I apply the "debit" rule handler with operation "debit" and amount 55.00
    Then the rule result should succeed
    And the source account available balance should be -45.00

  Scenario: Debit fails when amount exceeds available balance plus credit limit
    Given a source account with available balance 10.00, reserved balance 0.00 and credit limit 5.00
    When I apply the "debit" rule handler with operation "debit" and amount 20.00
    Then the rule result should fail with message containing "Insufficient funds. Available balance plus credit limit is insufficient for debit."

  Scenario: Debit succeeds when amount equals available balance plus credit limit exactly
    Given a source account with available balance 10.00, reserved balance 0.00 and credit limit 5.00
    When I apply the "debit" rule handler with operation "debit" and amount 15.00
    Then the rule result should succeed
    And the source account available balance should be -5.00

  Scenario: Debit fails when amount exceeds the limit by a small margin
    Given a source account with available balance 10.00, reserved balance 0.00 and credit limit 5.00
    When I apply the "debit" rule handler with operation "debit" and amount 15.01
    Then the rule result should fail

  Scenario: Credit then debit and then debit exceeding limit in processor flow
    Given an account with identification "ACC-001", available balance 0.00, reserved balance 0.00 and credit limit 500.00
    When I process a transaction with operation "credit", account id "ACC-001", amount 30000, currency "BRL" and reference id "TXN-CREDIT-001"
    Then the transaction status should be "Success"
    And the transaction available balance should be 30000
    When I process a transaction with operation "debit", account id "ACC-001", amount 60000, currency "BRL" and reference id "TXN-DEBIT-001"
    Then the transaction status should be "Success"
    And the transaction available balance should be -30000
    When I process a transaction with operation "debit", account id "ACC-001", amount 30000, currency "BRL" and reference id "TXN-DEBIT-002"
    Then the transaction status should be "Failed"
    And the transaction error message should contain "Insufficient funds"
