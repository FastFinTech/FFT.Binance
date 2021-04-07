// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Text.Json.Serialization;
  using FFT.TimeStamps;

  /// <summary>
  /// Trades that fill at the time, from the same taker order, with the same
  /// price will have the quantity aggregated. Returned by the <see
  /// cref="BinanceApiClient.GetAggregateTrades(string, TimeStamp,
  /// TimeStamp)"/> method.
  /// </summary>
  public sealed record AggregateTrade
    {
      [JsonPropertyName("a")]
      public long AggregateTradeId { get; init; }

      [JsonPropertyName("p")]
      public decimal Price { get; init; }

      [JsonPropertyName("q")]
      public decimal Quantity { get; init; }

      [JsonPropertyName("f")]
      public long FirstTradeId { get; init; }

      [JsonPropertyName("l")]
      public long LastTradeId { get; init; }

      [JsonPropertyName("T")]
      public TimeStamp Timestamp { get; init; }

      [JsonPropertyName("m")]
      public bool IsBuyerMarketMaker { get; init; }

      [JsonPropertyName("M")]
      public bool IsTradeBestPriceMatch { get; init; }
    }

  /*
  "e": "aggTrade",  // Event type
  "E": 123456789,   // Event time
  "s": "BNBBTC",    // Symbol
   * */

}
