// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  using System.Text.Json.Serialization;
  using FFT.TimeStamps;

  public sealed class TopOrderBook
  {
    [JsonConstructor]
    public TopOrderBook(
      string symbol,
      decimal bidPrice,
      decimal bidQty,
      decimal askPrice,
      decimal askQty)
    {
      Symbol = symbol;
      BidPrice = bidPrice;
      BidQty = bidQty;
      AskPrice = askPrice;
      AskQty = askQty;
    }

    public string Symbol { get; }
    public decimal BidPrice { get; }
    public decimal BidQty { get; }
    public decimal AskPrice { get; }
    public decimal AskQty { get; }
  }
}
