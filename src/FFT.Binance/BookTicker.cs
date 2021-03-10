// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Text.Json.Serialization;

  /// <summary>
  /// Supplied by the book ticker streams of type <see
  /// cref="StreamType.BookTicker"/> and <see
  /// cref="StreamType.BookTickerAllMarkets"/>.
  /// </summary>
  public sealed record BookTicker
  {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [JsonPropertyName("u")]
    public long OrderBookUpdateId { get; init; }

    [JsonPropertyName("s")]
    public string Symbol { get; init; }

    [JsonPropertyName("b")]
    public decimal BestBidPrice { get; init; }

    [JsonPropertyName("B")]
    public decimal BestBidQty { get; init; }

    [JsonPropertyName("a")]
    public decimal BestAskPrice { get; init; }

    [JsonPropertyName("A")]
    public decimal BestAskQty { get; init; }
  }
}
