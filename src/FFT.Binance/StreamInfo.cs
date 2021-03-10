// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Runtime.CompilerServices;
  using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

  public sealed class StreamInfo
  {
    private StreamInfo(string name, StreamType type, Type messageType)
    {
      Name = name.EnsureNotNullOrWhiteSpace(nameof(name));
      Type = type;
      MessageType = messageType.EnsureNotNull(nameof(messageType));
    }

    public string Name { get; }

    public StreamType Type { get; }

    public Type MessageType { get; }

    //public static StreamInfo AggregatedTrade(string symbol)
    //{
    //  return new StreamInfo($"{symbol.EnsureStreamSymbol()}@aggTrade", StreamType.AggregatedTrade);
    //}

    //public static StreamInfo RawTrade(string symbol)
    //  => new StreamInfo($"{symbol.EnsureStreamSymbol()}@trade", StreamType.RawTrade);

    //public static StreamInfo MiniTicker(string symbol)
    //  => new StreamInfo($"{symbol.EnsureStreamSymbol()}@miniTicker", StreamType.MiniTicker);

    //public static StreamInfo MiniTickerAllMarkets()
    //  => new StreamInfo("!miniTicker@arr", StreamType.MiniTickerAllMarkets);

    //public static StreamInfo Ticker(string symbol)
    //  => new StreamInfo($"{symbol.EnsureStreamSymbol()}@ticker", StreamType.Ticker);

    //public static StreamInfo TickerAllMarkets()
    //  => new StreamInfo("!ticker@arr", StreamType.TickerAllMarkets);

    public static StreamInfo BookTicker(string symbol)
      => new StreamInfo($"{symbol.EnsureStreamSymbol()}@bookTicker", StreamType.BookTicker, typeof(BookTicker));

    public static StreamInfo BookTickerAllMarkets()
      => new StreamInfo("!bookTicker", StreamType.BookTickerAllMarkets, typeof(BookTicker));

    //public static StreamInfo PartialDepth(string symbol, int numLevels, bool rapid)
    //{
    //  numLevels.EnsureIs(nameof(numLevels), "must be either 5, 10, or 20.", x => x is 5 or 10 or 20);
    //  var name = $"{symbol.EnsureStreamSymbol()}@depth{numLevels}";
    //  if (rapid)
    //    name += "@100ms";

    //  return new StreamInfo(name, StreamType.PartialDepth);
    //}

    public static StreamInfo FullDepth(string symbol, bool rapid)
    {
      var name = $"{symbol.EnsureStreamSymbol()}@depth";
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
