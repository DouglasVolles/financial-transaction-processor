Feature: Reversal transaction rule handler
  As the reversal transaction rule
  I want to undo a previous debit or credit transaction
  So that erroneous charges or deposits can be corrected

  Scenario: Reversal fails when no previous transaction exists
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And no previous transaction is defined
    When I apply the "reversal" rule handler with operation "reversal" and amount 50.00
    Then the rule result should fail with message containing "Reversal requires a previous successful transaction"

  Scenario: Reversal fails when amount does not match the last transaction
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And the last transaction was a "debit" of 50.00
    When I apply the "reversal" rule handler with operation "reversal" and amount 30.00
    Then the rule result should fail with message containing "Reversal amount must match"

  Scenario: Reversal of a debit increases the available balance
    Given a source account with available balance 70.00, reserved balance 0.00 and credit limit 0.00
    And the last transaction was a "debit" of 30.00
    When I apply the "reversal" rule handler with operation "reversal" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 100.00

  Scenario: Reversal of a credit decreases the available balance
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And the last transaction was a "credit" of 30.00
    When I apply the "reversal" rule handler with operation "reversal" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 70.00

  Scenario: Reversal of a credit fails when available balance is insufficient
    Given a source account with available balance 20.00, reserved balance 0.00 and credit limit 0.00
    And the last transaction was a "credit" of 30.00
    When I apply the "reversal" rule handler with operation "reversal" and amount 30.00
    Then the rule result should fail with message containing "Insufficient available balance to reverse"

  Scenario: Reversal fails for an unsupported previous operation type
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And the last transaction was a "reserve" of 30.00
    When I apply the "reversal" rule handler with operation "reversal" and amount 30.00
    Then the rule result should fail with message containing "Reversal is only supported for previous debit or credit"

  Scenario: Reversal succeeds in processor flow when previous successful debit exists
    Given an account with identification "ACC-REVERSAL", available balance 70.00, reserved balance 0.00 and credit limit 0.00
    And a previous successful transaction with operation "debit", amount 30.00, currency "BRL" and reference id "TXN-DEBIT-EARLIER" for account "ACC-REVERSAL"
    When I process a transaction with operation "reversal", account id "ACC-REVERSAL", amount 3000, currency "BRL" and reference id "TXN-REVERSAL-OK"
    Then the transaction status should be "Success"
    And the transaction available balance should be 10000

