// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance
{
  using System;
  using System.Runtime.CompilerServices;
  using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

  public sealed class StreamInfo
  {
    private StreamInfo(string name, StreamType type)
    {
      Name = name.EnsureNotNullOrWhiteSpace(nameof(name));
      Type = type;
    }

    public string Name { get; }

    public StreamType Type { get; }

    public static StreamType ParseStreamType(string streamId)
    {
      return streamId switch
      {
        "!miniTicker@arr" => StreamType.MiniTickerAllMarkets,
        "!ticker@arr" => StreamType.TickerAllMarkets,
        "!bookTicker" => StreamType.BookTickerAllMarkets,
        var a when a.EndsWith("@aggTrade") => StreamType.AggregatedTrade,
        var a when a.EndsWith("@trade") => StreamType.RawTrade,
        var a when a.EndsWith("@miniTicker") => StreamType.MiniTicker,
        var a when a.EndsWith("@ticker") => StreamType.Ticker,
        var a when a.EndsWith("@bookTicker") => StreamType.BookTicker,
        var a when a.EndsWith("@depth") => StreamType.FullDepth,
        var a when a.EndsWith("@depth@100ms") => StreamType.FullDepth,
        var a when a.Contains("@depth") => StreamType.PartialDepth, // @depth<levels> or @depth<levels>@100ms
        _ => throw new Exception($"Not able to parse a {typeof(StreamType)} value from '{streamId}'."),
      };
    }

    public static StreamInfo AggregatedTrade(string symbol)
      => new StreamInfo($"{symbol.EnsureStreamSymbol()}@aggTrade", StreamType.AggregatedTrade);

    public static StreamInfo RawTrade(string symbol)
      => new StreamInfo($"{symbol.EnsureStreamSymbol()}@trade", StreamType.RawTrade);

    public static StreamInfo MiniTicker(string symbol)
      => new StreamInfo($"{symbol.EnsureStreamSymbol()}@miniTicker", StreamType.MiniTicker);

    public static StreamInfo MiniTickerAllMarkets()
      => new StreamInfo("!miniTicker@arr", StreamType.MiniTickerAllMarkets);

    public static StreamInfo Ticker(string symbol)
      => new StreamInfo($"{symbol.EnsureStreamSymbol()}@ticker", StreamType.Ticker);

    public static StreamInfo TickerAllMarkets()
      => new StreamInfo("!ticker@arr", StreamType.TickerAllMarkets);

    public static StreamInfo BookTicker(string symbol)
      => new StreamInfo($"{symbol.EnsureStreamSymbol()}@bookTicker", StreamType.BookTicker);

    public static StreamInfo BookTickerAllMarkets()
      => new StreamInfo("!bookTicker", StreamType.BookTickerAllMarkets);

    public static StreamInfo PartialDepth(string symbol, int numLevels, bool rapid)
    {
      numLevels.EnsureIs(nameof(numLevels), "must be either 5, 10, or 20.", x => x is 5 or 10 or 20);
      var name = $"{symbol.EnsureStreamSymbol()}@depth{numLevels}";
      if (rapid)
        name += "@100ms";

      return new StreamInfo(name, StreamType.PartialDepth);
    }

    public static StreamInfo FullDepth(string symbol, bool rapid)
    {
      var name = $"{symbol.EnsureStreamSymbol()}@depth";
      if (rapid)
        name += "@100ms";

      return new StreamInfo(name, StreamType.FullDepth);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Name.Equals((obj as StreamInfo)?.Name);

    /// <inheritdoc/>
    public override int GetHashCode() => Name.GetHashCode();
  }
}
