// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.BinanceTickProviders
{
  using System.Runtime.CompilerServices;
  using FFT.Market.Ticks;

  internal static class Extensions
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tick AsTick(this AggregateTrade trade, decimal tickSizeAsDecimal)
      => new Tick
      {
        Price = (double)trade.Price,
        Bid = trade.IsBuyerMarketMaker ? (double)trade.Price : (double)(trade.Price - tickSizeAsDecimal),
        Ask = trade.IsBuyerMarketMaker ? (double)(trade.Price + tickSizeAsDecimal) : (double)trade.Price,
        Volume = (double)trade.Quantity,
        TimeStamp = trade.Timestamp,
      };
  }
}
