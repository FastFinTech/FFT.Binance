// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.MarketDataStreams
{
  using System;

  public sealed class StreamInfo
  {
    // TODO: remove the default "null" from the message type, that was added for
    // temporary compiler happiness.
    protected StreamInfo(string name, StreamType type, Type messageType = null)
    {
      Name = name;
      Type = type;
      MessageType = messageType;
    }

    public string Name { get; }

    public StreamType Type { get; }

    public Type MessageType { get; }

    public static StreamInfo AggregatedTrade(string symbol)
      => new StreamInfo($"{symbol}@aggTrade", StreamType.AggregatedTrade);

    public static StreamInfo RawTrade(string symbol)
      => new StreamInfo($"{symbol}@trade", StreamType.RawTrade);

    public static StreamInfo MiniTicker(string symbol)
      => new StreamInfo($"{symbol}@miniTicker", StreamType.MiniTicker);

    public static StreamInfo MiniTickerAllMarkets()
      => new StreamInfo("!miniTicker@arr", StreamType.MiniTickerAllMarkets);

    public static StreamInfo Ticker(string symbol)
      => new StreamInfo($"{symbol}@ticker", StreamType.Ticker);

    public static StreamInfo TickerAllMarkets()
      => new StreamInfo("!ticker@arr", StreamType.TickerAllMarkets);

    public static StreamInfo BookTicker(string symbol)
      => new StreamInfo($"{symbol}@bookTicker", StreamType.BookTicker);

    public static StreamInfo BookTickerAllMarkets()
      => new StreamInfo("!bookTicker", StreamType.BookTickerAllMarkets);

    public static StreamInfo PartialDepth(string symbol, int numLevels, bool rapid)
    {
      switch(numLevels)
      {
        case 5:
        case 10:
        case 20:
          break;
        default:
          throw new ArgumentException(nameof(numLevels));
      }

      var name = $"{symbol}@depth{numLevels}";
      if (rapid)
        name += "@100ms";

      return new StreamInfo(name, StreamType.PartialDepth);
    }

    public static StreamInfo FullDepth(string symbol, bool rapid)
    {
      var name = $"{symbol}@depth";
      if (rapid)
        name += "@100ms";

      return new StreamInfo(name, StreamType.FullDepth, typeof(Book));
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Name.Equals((obj as StreamInfo)?.Name);

    /// <inheritdoc/>
    public override int GetHashCode() => Name.GetHashCode();
  }
}
