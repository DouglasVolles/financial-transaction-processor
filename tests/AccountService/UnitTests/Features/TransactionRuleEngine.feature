Feature: Transaction rule engine
  As the transaction rule engine
  I want to route each transaction operation to the correct rule handler
  So that all operation types are processed by their dedicated business rules

  Scenario: Engine routes a credit operation to the credit handler
    Given a source account with available balance 50.00, reserved balance 0.00 and credit limit 0.00
    When I apply the rule engine with operation "credit" and amount 20.00
    Then the rule result should succeed
    And the source account available balance should be 70.00

  Scenario: Engine routes a debit operation to the debit handler
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I apply the rule engine with operation "debit" and amount 40.00
    Then the rule result should succeed
    And the source account available balance should be 60.00

  Scenario: Engine routes a reserve operation to the reserve handler
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I apply the rule engine with operation "reserve" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 70.00
    And the source account reserved balance should be 30.00

  Scenario: Engine routes a capture operation to the capture handler
    Given a source account with available balance 0.00, reserved balance 50.00 and credit limit 0.00
    When I apply the rule engine with operation "capture" and amount 50.00
    Then the rule result should succeed
    And the source account available balance should be 0.00
    And the source account reserved balance should be 0.00

  Scenario: Engine routes a reversal operation to the reversal handler
    Given a source account with available balance 70.00, reserved balance 0.00 and credit limit 0.00
    And the last transaction was a "debit" of 30.00
    When I apply the rule engine with operation "reversal" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 100.00

  Scenario: Engine routes a transfer operation to the transfer handler
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And a destination account with available balance 25.00
    When I apply the rule engine with operation "transfer" and amount 40.00
    Then the rule result should succeed
    And the source account available balance should be 60.00
    And the destination account available balance should be 65.00
