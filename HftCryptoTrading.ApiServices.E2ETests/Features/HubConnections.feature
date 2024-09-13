Feature: Hub Connection Scenarios

  Scenario: Client connects and joins a non-existing group
    Given a client is connected for the namespace "orders" and event "created"
    When the client publishes a message "123e4567-e89b-12d3-a456-426614174000" to namespace "orders" and event "created"
    Then the client should receive a delayed notification
    When another client subscribes for the namespace "orders" and event "created"
    Then the second client should receive the pending message
    And the first client should receive a message distribution notification