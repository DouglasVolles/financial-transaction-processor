Feature: Capture transaction rule handler
  As the capture transaction rule
  I want to settle reserved funds after authorization
  So that reserved amounts are settled after a successful capture

  Scenario: Capture settles reserved balance without changing available balance
    Given a source account with available balance 0.00, reserved balance 50.00 and credit limit 0.00
    When I apply the "capture" rule handler with operation "capture" and amount 50.00
    Then the rule result should succeed
    And the source account available balance should be 0.00
    And the source account reserved balance should be 0.00

  Scenario: Capture fails when amount does not match the reserved balance
    Given a source account with available balance 0.00, reserved balance 30.00 and credit limit 0.00
    When I apply the "capture" rule handler with operation "capture" and amount 50.00
    Then the rule result should fail with message containing "Capture amount must be equal"

  Scenario: Capture fails when no amount is reserved
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I apply the "capture" rule handler with operation "capture" and amount 50.00
    Then the rule result should fail with message containing "Capture amount must be equal"

  Scenario: Balances remain unchanged when capture fails
    Given a source account with available balance 100.00, reserved balance 30.00 and credit limit 0.00
    When I apply the "capture" rule handler with operation "capture" and amount 50.00
    Then the rule result should fail
    And the source account available balance should be 100.00
    And the source account reserved balance should be 30.00