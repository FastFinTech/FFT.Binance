// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1516 // Elements should be separated by blank line

  using System.Text.Json.Serialization;

  public sealed class LastPrice
  {
    [JsonConstructor]
    public LastPrice(
      string symbol,
      decimal price)
    {
      Symbol = symbol;
      Price = price;
    }

    public string Symbol { get; }
    public decimal Price { get; }
  }
}
