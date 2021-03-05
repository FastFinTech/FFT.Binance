// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.TickerStreams
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

  using System.Text.Json.Serialization;
  using FFT.TimeStamps;

  public sealed class DepthStreamDiff
  {
    [JsonConstructor]
    public DepthStreamDiff(
      string eventType,
      TimeStamp eventTime,
      string symbol,
      long updateIdFrom,
      long updateIdTo,
      decimal[][] bids,
      decimal[][] asks)
    {
      EventType = eventType;
      EventTime = eventTime;
      Symbol = symbol;
      UpdateIdFrom = updateIdFrom;
      UpdateIdTo = updateIdTo;
      Bids = bids;
      Asks = asks;
    }

    [JsonPropertyName("e")]
    public string EventType { get; } // "depthUpdate"

    [JsonPropertyName("E")]
    public TimeStamp EventTime { get; }

    [JsonPropertyName("s")]
    public string Symbol { get; }

    [JsonPropertyName("U")]
    public long UpdateIdFrom { get; }

    [JsonPropertyName("u")]
    public long UpdateIdTo { get; }

    [JsonPropertyName("b")]
    public decimal[][] Bids { get; }

    [JsonPropertyName("a")]
    public decimal[][] Asks { get; }
  }
}
