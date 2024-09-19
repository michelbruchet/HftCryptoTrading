Feature: Retrieve symbols, historical klines, and tickers from the Binance API and rank the Top 50 symbols

  As a trader,
  I want to retrieve symbols, historical klines, and tickers from the Binance API
  So that I can rank the Top 50 symbols based on market data for informed decision making.

  Background:
    Given the Binance API is available

  Scenario: Retrieve symbols from Binance API
    When I request the available symbols from the Binance API
    Then I should receive a list of all trading pairs (symbols)

  Scenario: Retrieve historical klines for each symbol
    Given I have a list of trading pairs from the Binance API
    When I request historical klines data for each symbol
    Then I should receive OHLC (Open, High, Low, Close) data for a specified time range

  Scenario: Retrieve current tickers for each symbol
    Given I have a list of trading pairs from the Binance API
    When I request the current ticker data for each symbol
    Then I should receive the current price, volume, and 24-hour percentage change for each symbol

  Scenario: Rank Top 50 symbols by trading volume
    Given I have retrieved current tickers and historical data for each symbol
    When I rank the symbols by 24-hour trading volume
    Then I should see the top 50 symbols ranked by trading volume in descending order

  Scenario: Handle API rate limits
    Given the Binance API rate limit is enforced
    When I make too many requests in a short period
    Then I should wait until the rate limit resets
    And I should retry the request after the rate limit has reset

  Scenario: Handle invalid symbols
    Given there are symbols that might no longer be available
    When I request data for an invalid symbol
    Then I should skip the symbol and log an error without disrupting the process
