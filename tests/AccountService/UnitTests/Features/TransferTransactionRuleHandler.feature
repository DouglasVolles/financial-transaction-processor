Feature: Transfer transaction rule handler
  As the transfer transaction rule
  I want to move funds from a source account to a destination account
  So that peer-to-peer transfers are processed correctly

  Scenario: Transfer fails when no destination account is specified
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And no destination account is specified
    When I apply the "transfer" rule handler with operation "transfer" and amount 50.00
    Then the rule result should fail with message containing "Destination account is required"

  Scenario: Transfer fails when source has insufficient funds
    Given a source account with available balance 20.00, reserved balance 0.00 and credit limit 10.00
    And a destination account with available balance 0.00
    When I apply the "transfer" rule handler with operation "transfer" and amount 35.00
    Then the rule result should fail with message containing "Insufficient funds"

  Scenario: Transfer deducts from source and adds to destination
    Given a source account with available balance 100.00, reserved balance 0.00 and credit limit 0.00
    And a destination account with available balance 25.00
    When I apply the "transfer" rule handler with operation "transfer" and amount 40.00
    Then the rule result should succeed
    And the source account available balance should be 60.00
    And the destination account available balance should be 65.00

  Scenario: Transfer succeeds using the source credit limit
    Given a source account with available balance 10.00, reserved balance 0.00 and credit limit 50.00
    And a destination account with available balance 0.00
    When I apply the "transfer" rule handler with operation "transfer" and amount 55.00
    Then the rule result should succeed
    And the source account available balance should be -45.00
    And the destination account available balance should be 55.00

  Scenario: Transfer fails when amount slightly exceeds available plus credit limit
    Given a source account with available balance 10.00, reserved balance 0.00 and credit limit 5.00
    And a destination account with available balance 0.00
    When I apply the "transfer" rule handler with operation "transfer" and amount 15.01
    Then the rule result should fail

  Scenario: Transfer fails in processor flow when destination account id is missing
    Given an account with identification "ACC-SRC", available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I process a transfer transaction with account id "ACC-SRC", destination account id "null", amount 5000, currency "BRL" and reference id "TXN-TRANSFER-NO-DEST"
    Then the transaction status should be "Failed"
    And the transaction error message should contain "DestinationAccountId is required"

  Scenario: Transfer fails in processor flow when destination account is not found
    Given an account with identification "ACC-SRC2", available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I process a transfer transaction with account id "ACC-SRC2", destination account id "ACC-404", amount 5000, currency "BRL" and reference id "TXN-TRANSFER-DEST-404"
    Then the transaction status should be "Failed"
    And the transaction error message should be "Destination account not found"

  Scenario: Transfer fails in processor flow when destination equals source
    Given an account with identification "ACC-SELF", available balance 100.00, reserved balance 0.00 and credit limit 0.00
    When I process a transfer transaction with account id "ACC-SELF", destination account id "ACC-SELF", amount 5000, currency "BRL" and reference id "TXN-TRANSFER-SELF"
    Then the transaction status should be "Failed"
    And the transaction error message should contain "Destination account must be different from source"

  Scenario: Transfer between two accounts of the same customer
    Given an account with identification "ACC-001", available balance 0.00, reserved balance 0.00 and credit limit 0.00
    And an additional account for the same customer with identification "ACC-002", available balance 0.00, reserved balance 0.00 and credit limit 0.00
    When I process a transfer transaction with account id "ACC-001", destination account id "ACC-002", amount 50000, currency "BRL" and reference id "TXN-CREDIT-TRANSFER-SETUP"
    Then the transaction status should be "Failed"
    When I process a transaction with operation "credit", account id "ACC-001", amount 50000, currency "BRL" and reference id "TXN-CREDIT-FOR-TRANSFER"
    Then the transaction status should be "Success"
    When I process a transfer transaction with account id "ACC-001", destination account id "ACC-002", amount 50000, currency "BRL" and reference id "TXN-TRANSFER-SAME-CUSTOMER"
    Then the transaction status should be "Success"
    And the persisted account with identification "ACC-001" available balance should be 0.00
    And the persisted account with identification "ACC-002" available balance should be 500.00
