// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.Binance.MarketDataStreams
{
  public enum StreamType
  {
    /// <summary>
    /// Contains trade information aggregated for a single taker order for a
    /// specific instrument.
    /// </summary>
    AggregatedTrade,

    /// <summary>
    /// Contains raw trade information for a specific instrument.
    /// </summary>
    RawTrade,

    /// <summary>
    /// Contains the mini ticker for a specific instrument.
    /// </summary>
    MiniTicker,

    /// <summary>
    /// Contains the mini ticker for all instruments.
    /// </summary>
    MiniTickerAllMarkets,

    /// <summary>
    /// Contains the full ticker info for a specific instrument.
    /// </summary>
    Ticker,

    /// <summary>
    /// Contains the full ticker info for all instruments.
    /// </summary>
    TickerAllMarkets,

    /// <summary>
    /// Contains the top book ticker info for a specific instrument.
    /// </summary>
    BookTicker,

    /// <summary>
    /// Contains the top book ticker info for all instruments.
    /// </summary>
    BookTickerAllMarkets,

    /// <summary>
    /// Contains the first "x" levels order book for a specific instrument.
    /// </summary>
    PartialDepth,

    /// <summary>
    /// Contains the full order book for a specific instrument.
    /// </summary>
    FullDepth,
  }
}
