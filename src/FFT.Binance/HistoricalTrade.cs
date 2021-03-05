// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1516 // Elements should be separated by blank line

  using System.Text.Json.Serialization;
  using FFT.TimeStamps;

  public sealed class HistoricalTrade
  {
    [JsonConstructor]
    public HistoricalTrade(
      long id,
      decimal price,
      decimal qty,
      decimal quoteQty,
      TimeStamp time,
      bool isBuyerMaker,
      bool isBestMatch)
    {
      Id = id;
      Price = price;
      Qty = qty;
      QuoteQty = quoteQty;
      Time = time;
      IsBuyerMaker = isBuyerMaker;
      IsBestMatch = isBestMatch;
    }

    public long Id { get; }
    public decimal Price { get; }
    public decimal Qty { get; }
    public decimal QuoteQty { get; }
    public TimeStamp Time { get; }

    /// <summary>
    /// True if the buyer was the maker.
    /// </summary>
    public bool IsBuyerMaker { get; }

    /// <summary>
    /// True if the trade was the best price match.
    /// </summary>
    public bool IsBestMatch { get; }
  }
}
