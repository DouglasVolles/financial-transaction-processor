Feature: Reserve transaction rule handler
  As the reserve transaction rule
  I want to move funds from available to reserved balance
  So that amounts earmarked for upcoming operations are properly held

  Scenario: Reserve moves amount from available to reserved balance
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I apply the "reserve" rule handler with operation "reserve" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 70.00
    And the source account reserved balance should be 30.00

  Scenario: Reserve succeeds when amount equals the full available balance
    Given a source account with available balance 30.00, reserved balance 0.00 and credit limit 0.00
    When I apply the "reserve" rule handler with operation "reserve" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 0.00
    And the source account reserved balance should be 30.00

  Scenario: Reserve fails when amount exceeds available balance ignoring credit limit
    Given a source account with available balance 20.00, reserved balance 0.00 and credit limit 100.00
    When I apply the "reserve" rule handler with operation "reserve" and amount 50.00
    Then the rule result should fail with message containing "Insufficient available balance"

  Scenario: Reserve accumulates on top of an existing reserved balance
    Given a source account with available balance 100.00, reserved balance 20.00 and credit limit 0.00
    When I apply the "reserve" rule handler with operation "reserve" and amount 30.00
    Then the rule result should succeed
    And the source account available balance should be 70.00
    And the source account reserved balance should be 50.00

