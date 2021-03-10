// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.MarketDataStreams
{
  using System.Collections.Generic;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  using System.Collections.Immutable;
  using System.Linq;
  using System.Runtime.InteropServices;
  using FFT.TimeStamps;

  public sealed record Book
  {
    public TimeStamp LastUpdateTime { get; init; }

    public long LastUpdateId { get; init; }

    public ImmutableDictionary<decimal, decimal> Bids { get; init; }

    public ImmutableDictionary<decimal, decimal> Asks { get; init; }

    /// <remarks>
    /// Don't forget to dispose the <paramref name="diff"/> after using it.
    /// </remarks>
    internal Book ApplyDiff(ref BookUpdate diff)
    {
      var ladderUpdate = MemoryMarshal.ToEnumerable<(decimal Price, decimal Qty)>(diff.Bids.Memory);
      var bids = Bids
        .RemoveRange(ladderUpdate.Where(x => x.Qty == 0).Select(x => x.Price))
        .SetItems(ladderUpdate.Where(x => x.Qty > 0).Select(x => new KeyValuePair<decimal, decimal>(x.Price, x.Qty)));

      ladderUpdate = MemoryMarshal.ToEnumerable<(decimal Price, decimal Qty)>(diff.Asks.Memory);
      var asks = Asks
        .RemoveRange(ladderUpdate.Where(x => x.Qty == 0).Select(x => x.Price))
        .SetItems(ladderUpdate.Where(x => x.Qty > 0).Select(x => new KeyValuePair<decimal, decimal>(x.Price, x.Qty)));

      return this with
      {
        Asks = asks,
        Bids = bids,
        LastUpdateId = diff.UpdateIdTo,
        LastUpdateTime = diff.EventTime,
      };
    }

    internal static Book From(OrderBookResponse response)
    {
      var bids = ImmutableDictionary<decimal, decimal>.Empty
        .AddRange(response.Bids.Select(x => new KeyValuePair<decimal, decimal>(x.Price, x.Qty)));

      var asks = ImmutableDictionary<decimal, decimal>.Empty
        .AddRange(response.Asks.Select(x => new KeyValuePair<decimal, decimal>(x.Price, x.Qty)));

      return new Book
      {
        Bids = bids,
        Asks = asks,
        LastUpdateId = response.LastUpdateId,
        LastUpdateTime = TimeStamp.Now,
      };
    }
  }
}
