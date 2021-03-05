// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System.Collections.Immutable;
  using System.Text.Json.Serialization;

  /// <summary>
  /// Returned by the <see cref="BinanceApiClient.GetOrderBook(string, int)"/> method.
  /// </summary>
  public sealed class OrderBookResponse
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBookResponse"/> class.
    /// </summary>
    [JsonConstructor]
    public OrderBookResponse(
      long lastUpdateId,
      ImmutableList<PriceQty> bids,
      ImmutableList<PriceQty> asks)
    {
      LastUpdateId = lastUpdateId;
      Bids = bids;
      Asks = asks;
    }

    /// <summary>
    /// Id of the most recent order book update.
    /// </summary>
    public long LastUpdateId { get; }

    /// <summary>
    /// The top bids in the order book.
    /// </summary>
    public ImmutableList<PriceQty> Bids { get; }

    /// <summary>
    /// The top asks in the order book.
    /// </summary>
    public ImmutableList<PriceQty> Asks { get; }
  }
}
