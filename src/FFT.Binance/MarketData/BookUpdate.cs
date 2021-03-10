// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.MarketDataStreams
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Text.Json.Serialization;
  using FFT.Binance.Serialization;
  using FFT.TimeStamps;
  using LadderMemoryOwner = System.Buffers.IMemoryOwner<(decimal Price, decimal Qty)>;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter


  /// <summary>
  /// Supplied in a <see cref="StreamType.FullDepth"/> stream.
  /// </summary>
  /// <remarks>
  /// Book update objects are created frequently and used only briefly for
  /// updating <see cref="Book"/> objects. To save allocations, they use rented
  /// memory to store the bid and ask updates. The memory must be returned to a
  /// store, so you must remember to dispose these objects to prevent a memory
  /// leak. 
  /// </remarks>
  internal struct BookUpdate : IDisposable
  {
    [JsonPropertyName("e")]
    public string EventType { get; init; } // always "depthUpdate"

    [JsonPropertyName("E")]
    public TimeStamp EventTime { get; init; }

    [JsonPropertyName("s")]
    public string Symbol { get; init; }

    [JsonPropertyName("U")]
    public long UpdateIdFrom { get; init; }

    [JsonPropertyName("u")]
    public long UpdateIdTo { get; init; }

    [JsonPropertyName("b")]
    public LadderMemoryOwner Bids { get; init; }

    [JsonPropertyName("a")]
    public LadderMemoryOwner Asks { get; init; }

    public void Dispose()
    {
      Bids.Dispose();
      Asks.Dispose();
    }
  }
}
