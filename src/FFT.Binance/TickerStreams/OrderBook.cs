// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.TickerStreams
{
  using System.Collections.Generic;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  using System.Collections.Immutable;
  using System.Linq;
  using FFT.TimeStamps;

  public sealed record OrderBook
  {
    public TimeStamp LastUpdateTime { get; init; }

    public long LastUpdateId { get; init; }

    public ImmutableDictionary<decimal, decimal> Bids { get; init; }

    public ImmutableDictionary<decimal, decimal> Asks { get; init; }

    public OrderBook ApplyDiff(DepthStreamDiff diff)
    {
      var bids = Bids;
      var asks = Asks;
      foreach (var bid in diff.Bids)
        bids = bid[1] == 0 ? bids.Remove(bid[0]) : bids.SetItem(bid[0], bid[1]);
      foreach (var ask in diff.Asks)
        asks = ask[1] == 0 ? asks.Remove(ask[0]) : asks.SetItem(ask[0], ask[1]);
      return this with
      {
        Asks = asks,
        Bids = bids,
        LastUpdateId = diff.UpdateIdTo,
        LastUpdateTime = diff.EventTime,
      };
    }

    public static OrderBook From(OrderBookResponse response)
    {
      var bids = ImmutableDictionary<decimal, decimal>.Empty
        .AddRange(response.Bids.Select(x => new KeyValuePair<decimal, decimal>(x.Price, x.Qty)));

      var asks = ImmutableDictionary<decimal, decimal>.Empty
        .AddRange(response.Asks.Select(x => new KeyValuePair<decimal, decimal>(x.Price, x.Qty)));

      return new OrderBook
      {
        Bids = bids,
        Asks = asks,
        LastUpdateId = response.LastUpdateId,
        LastUpdateTime = TimeStamp.Now,
      };
    }
  }
}
